namespace AlwaysDecrypted.Data
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Threading.Tasks;
    using AlwaysDecrypted.Logging;
    using AlwaysDecrypted.Models;
    using Dapper;

    public class ColumnEncryptionRepository : IColumnEncryptionRepository
	{
		private const int BatchSize = 10000;
		private IConnectionFactory ConnectionFactory { get; }
		private IColumnEncryptionQueryFactory QueryFactory { get; }
		private IPrimaryKeyValidationService PrimaryKeyValidationService { get; }
		private ILogger Logger { get; }

		public ColumnEncryptionRepository(
			IConnectionFactory connectionFactory, 
			IColumnEncryptionQueryFactory queryFactory,
			IPrimaryKeyValidationService primaryKeyValidationService,
			ILogger logger)
		{
			ConnectionFactory = connectionFactory;
			QueryFactory = queryFactory;
			PrimaryKeyValidationService = primaryKeyValidationService;
			Logger = logger;
		}

		public async Task DecryptColumns(Table table, IEnumerable<EncryptedColumn> columns)
		{
			// Get primary key columns
			var primaryKeyColumns = await this.GetPrimaryKeyColumns(table);

			// All tables with encrypted columns must have a primary key. 
			// Otherwise we cannot decrypt the data (at least currently - it would be possible with some further changes).
			this.PrimaryKeyValidationService.ValidatePrimaryKeyColumns(table, primaryKeyColumns);

			// Decrypt data the given table
			await this.DecryptDataForTable(columns, primaryKeyColumns);
		}

		public async Task<IEnumerable<EncryptedColumn>> GetEncryptedColumns()
		{
			using (var connection = this.ConnectionFactory.GetConnection())
			{
				return await connection.QueryAsync<EncryptedColumn>(this.QueryFactory.GetEncryptedColumnsSelectQuery());
			}
		}

		public async Task<IEnumerable<EncryptedColumn>> GetEncryptedColumns(Table table)
		{
			using (var connection = this.ConnectionFactory.GetConnection())
			{
				return await connection.QueryAsync<EncryptedColumn>(this.QueryFactory.GetEncryptedColumnsSelectQuery(), new { table.Schema, Table = table.Name });
			}
		}

		public async Task<IEnumerable<Table>> GetEncryptedTables(IEnumerable<Table> includedTables)
		{
			using(var connection = this.ConnectionFactory.GetConnection())
			{
				return await connection.QueryAsync<Table>(this.QueryFactory.GetEncryptedTablesQuery(includedTables));
			}
		}

		public async Task RenameColumnsForDecryption(IEnumerable<EncryptedColumn> columns)
		{
			using (var connection = this.ConnectionFactory.GetConnection())
			{
				foreach (var column in columns)
				{
					await connection.ExecuteAsync(this.QueryFactory.GetEncryptedColumnRenameQuery(column));
				}
			}
		}

		public async Task CreatePlainColumns(IEnumerable<EncryptedColumn> columns)
		{
			using (var connection = this.ConnectionFactory.GetConnection())
			{
				foreach (var column in columns)
				{
					await connection.ExecuteAsync(this.QueryFactory.GetPlainColumnCreateQuery(column));
				}
			}
		}

		/// <summary>
		/// Adds a decryption status column (IsDataDecrypted) to the table.
		/// 
		/// This column is not currently used as part of the decryption, but may be useful 
		/// in cases where the decryption fails half way through.
		/// </summary>
		/// <param name="tables">The table containing encrypted columns.</param>
		public async Task CreateDecryptionStatusColumn(Table table)
		{
			using (var connection = this.ConnectionFactory.GetConnection())
			{
				await connection.ExecuteAsync(this.QueryFactory.GetDecryptionStatusColumnCreateQuery(table));
			}
		}

		/// <summary>
		/// Cleans up temporary columns used for decryption, and removes the encrypted columns that have been decrypted.
		/// </summary>
		/// <param name="columns">The columns to be cleaned up.</param>
		public async Task CleanUpTable(IEnumerable<EncryptedColumn> columns)
		{
			using (var connection = this.ConnectionFactory.GetConnection())
			{
				await connection.ExecuteAsync(this.QueryFactory.GetCleanUpQuery(columns));
			}

			this.Logger.Log($"Cleaned up {columns.First().FullTableName} after decryption", LogEventLevel.Information);
		}

		private async Task DecryptDataForTable(IEnumerable<EncryptedColumn> encryptedColumns, IEnumerable<PrimaryKeyColumn> primaryKey)
		{
			using (var connection = this.ConnectionFactory.GetSqlConnection())
			{
				connection.Open();
				var batchNumber = 1;
				while (true)
				{
					var command = connection.CreateCommand();
					command.CommandText = this.QueryFactory.GetEncryptedDataSelectQuery(encryptedColumns, primaryKey);
					command.Parameters.Add(new SqlParameter("@BatchSize", BatchSize));
					command.Parameters.Add(new SqlParameter("@BatchNumber", batchNumber));
					var reader = await command.ExecuteReaderAsync();

					if (!reader.HasRows)
					{
						break;
					}

					await this.DecryptBatch(encryptedColumns, primaryKey, reader);
					batchNumber++;
				}
			}

			this.Logger.Log($"Finished decrypting data for {encryptedColumns.First().FullTableName}", LogEventLevel.Information);
		}

		/// <summary>
		/// Update records with decrypted values from the given data reader.
		/// </summary>
		/// <param name="encryptedColumns"></param>
		/// <param name="primaryKey"></param>
		/// <param name="reader"></param>
		private async Task DecryptBatch(IEnumerable<EncryptedColumn> encryptedColumns, IEnumerable<PrimaryKeyColumn> primaryKey, IDataReader reader)
		{
			using (var connection = this.ConnectionFactory.GetSqlConnection())
			{
				connection.Open();

				// Create a #temp table to bulk insert data into
				await this.CreateTempUpdateTable(encryptedColumns, primaryKey, connection);
				using (var bulkCopy = new SqlBulkCopy(connection))
				{
					this.AddBulkCopySettings(bulkCopy, encryptedColumns, primaryKey);

					try
					{
						// Bulk insert data into the #temp data
						await bulkCopy.WriteToServerAsync(reader);

						// Update data from the #temp table into the table being decrypted
						await connection.ExecuteAsync(this.QueryFactory.GetPlainValuesFromTempTableUpdateQuery(encryptedColumns, primaryKey));
					}
					catch (Exception ex)
					{
						this.Logger.Log(ex.Message, LogEventLevel.Error);
					}
					finally
					{
						reader.Close();
					}
				}
			}
		}

		private void AddBulkCopySettings(SqlBulkCopy bulkCopy, IEnumerable<Column> encryptedColumns, IEnumerable<Column> primaryKey)
		{
			bulkCopy.DestinationTableName = this.QueryFactory.GetTempUpdateTableName(encryptedColumns, primaryKey);
			bulkCopy.BatchSize = BatchSize;
			bulkCopy.BulkCopyTimeout = 60;
			this.AddBulkCopyColumnMappings(bulkCopy, encryptedColumns, primaryKey);
		}

		private void AddBulkCopyColumnMappings(SqlBulkCopy bulkCopy, IEnumerable<Column> encryptedColumns, IEnumerable<Column> primaryKey)
		{
			foreach(var column in encryptedColumns)
			{
				bulkCopy.ColumnMappings.Add($"{column.Name}_Encrypted", $"{column.Name}");
			}

			foreach (var column in primaryKey)
			{
				bulkCopy.ColumnMappings.Add($"{column.Name}", $"{column.Name}");
			}
		}

		private async Task CreateTempUpdateTable(IEnumerable<EncryptedColumn> encryptedColumns, IEnumerable<PrimaryKeyColumn> primaryKey, SqlConnection connection)
		{
			await connection.ExecuteAsync(this.QueryFactory.GetTempUpdateTableCreateQuery(encryptedColumns, primaryKey));
		}
		
		/// <summary>
		/// Gets primary key columns for a given table.
		/// </summary>
		/// <param name="table">The table.</param>
		private async Task<IEnumerable<PrimaryKeyColumn>> GetPrimaryKeyColumns(Table table)
		{
			using (var connection = this.ConnectionFactory.GetConnection())
			{
				return await connection.QueryAsync<PrimaryKeyColumn>(this.QueryFactory.GetSelectPrimaryKeyColumnsQuery(), new { Schema = table.Schema, Table = table.Name });
			}
		}
	}
}
