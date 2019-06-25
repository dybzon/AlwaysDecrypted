namespace AlwaysDecrypted.Data
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Threading.Tasks;
	using AlwaysDecrypted.Models;
    using Dapper;

    public class ColumnEncryptionRepository : IColumnEncryptionRepository
	{
		private const int BatchSize = 10000;
		private IConnectionFactory ConnectionFactory { get; }
		private IColumnEncryptionQueryFactory QueryFactory { get; }

		public ColumnEncryptionRepository(IConnectionFactory connectionFactory, IColumnEncryptionQueryFactory queryFactory)
		{
			ConnectionFactory = connectionFactory;
			QueryFactory = queryFactory;
		}

		public async Task DecryptColumns(IEnumerable<EncryptedColumn> columns)
		{
			// Group columns per table
			var tableGroups = columns.GroupBy(c => c.Table);

			// Get primary key columns for all tables that contain encrypted columns
			var primaryKeyColumns = (await Task.WhenAll(tableGroups.Select(async table => await this.GetPrimaryKeyColumns(table.First().Schema, table.Key)))).SelectMany(cols => cols);

			// All tables with encrypted columns must have a primary key. 
			// Otherwise we cannot decrypt the data (at least currently - it would be possible with some further changes).
			this.ValidatePrimaryKeyColumns(tableGroups, primaryKeyColumns);

			// Decrypt data for each table with encrypted columns
			await Task.WhenAll(tableGroups.Select(async table => await this.DecryptDataForTable(table, primaryKeyColumns.Where(c => c.Table.Equals(table.Key, StringComparison.InvariantCultureIgnoreCase)))));
		}

		public async Task<IEnumerable<EncryptedColumn>> GetEncryptedColumns()
		{
			using (var connection = this.ConnectionFactory.GetConnection())
			{
				return await connection.QueryAsync<EncryptedColumn>(this.QueryFactory.GetEncryptedColumnsSelectQuery());
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
		/// Adds a decryption status column (IsDataDecrypted) to each of the tables containing encrypted columns.
		/// 
		/// This column is not currently used as part of the decryption, but may be useful 
		/// in cases where the decryption fails half way through.
		/// </summary>
		/// <param name="tables">The tables containing encrypted columns.</param>
		public async Task CreateDecryptionStatusColumns(IEnumerable<(string, string)> tables)
		{
			foreach (var table in tables)
			{
				await this.CreateDecryptionStatusColumn(table.Item1, table.Item2);
			}
		}

		public async Task CreateDecryptionStatusColumn(string schemaName, string tableName)
		{
			using (var connection = this.ConnectionFactory.GetConnection())
			{
				await connection.ExecuteAsync(this.QueryFactory.GetDecryptionStatusColumnCreateQuery(schemaName, tableName));
			}
		}

		/// <summary>
		/// Cleans up temporary columns used for decryption, and removes the encrypted columns that have been decrypted.
		/// </summary>
		/// <param name="columns">The columns to be cleaned up.</param>
		public async Task CleanUpTables(IEnumerable<EncryptedColumn> columns)
		{
			await Task.WhenAll(columns.GroupBy(c => c.Table).Select(async table => await this.CleanUpTable(table)));
		}

		public async Task CleanUpTable(IEnumerable<EncryptedColumn> columns)
		{
			using (var connection = this.ConnectionFactory.GetConnection())
			{
				await connection.ExecuteAsync(this.QueryFactory.GetCleanUpQuery(columns));
			}
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
						Console.WriteLine(ex.Message);
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
		/// Gets primary key columns for a given table on a given schema.
		/// </summary>
		/// <param name="schemaName">The name of the schema.</param>
		/// <param name="tableName">The name of the table.</param>
		private async Task<IEnumerable<PrimaryKeyColumn>> GetPrimaryKeyColumns(string schemaName, string tableName)
		{
			using (var connection = this.ConnectionFactory.GetConnection())
			{
				return await connection.QueryAsync<PrimaryKeyColumn>(this.QueryFactory.GetSelectPrimaryKeyColumnsQuery(), new { Schema = schemaName, Table = tableName });
			}
		}

		/// <summary>
		/// All tables that contain encrypted columns must also contain a primary key.
		/// Otherwise we cannot decrypt the data from those tables.
		/// </summary>
		private void ValidatePrimaryKeyColumns(IEnumerable<IGrouping<string, EncryptedColumn>> tableGroups, IEnumerable<PrimaryKeyColumn> primaryKeyColumns)
		{
			// Throw if a table has no primary key
			var tablesWithoutPrimaryKey = tableGroups.Where(table => !primaryKeyColumns.Any(col => col.Table.Equals(table.Key, StringComparison.InvariantCultureIgnoreCase)));
			if (tablesWithoutPrimaryKey.Any())
			{
				var table = tablesWithoutPrimaryKey.First();
				throw new InvalidOperationException($"The table {table.First().FullTableName} has no primary key. Decrypting data in tables without a primary key is not currently supported.");
			}

		}
	}
}
