namespace AlwaysDecrypted.Data
{
    using System;
    using System.Collections.Generic;
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

			//var test = tableGroups.First();
			//await this.DecryptDataForTable(test, primaryKeyColumns.Where(c => c.Table.Equals(test.Key, StringComparison.InvariantCultureIgnoreCase)));
		}

		public async Task<IEnumerable<EncryptedColumn>> GetEncryptedColumns()
		{
			using (var connection = this.ConnectionFactory.GetConnection())
			{
				var columns = await connection.QueryAsync<EncryptedColumn>(this.QueryFactory.GetEncryptedColumnsSelectQuery());
				return columns;
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
			using (var connection = this.ConnectionFactory.GetConnection())
			{
				var batchNumber = 1;
				while (true)
				{
					// Fetch rows from the table one batch at a time until all records are decrypted
					var rows = (await connection.QueryAsync(
							this.QueryFactory.GetEncryptedDataSelectQuery(encryptedColumns, primaryKey),
							new { BatchSize, BatchNumber = batchNumber }))
						.Select(row => (IDictionary<string, object>)row); // The dynamic returned by Dapper is always a DapperRow, which is also a dictionary of <string, object>.

					if (!rows.Any())
					{
						// Assume we're done if no records were returned.
						break;
					}

					await this.UpdatePlainColumns(encryptedColumns, primaryKey, rows);
					batchNumber++;
				}
			}
		}

		private async Task UpdatePlainColumns(IEnumerable<EncryptedColumn> encryptedColumns, IEnumerable<PrimaryKeyColumn> primaryKey, IEnumerable<IDictionary<string, object>> rows)
		{
			var updateQuery = this.QueryFactory.GetPlainColumnsUpdateQuery(encryptedColumns, primaryKey);
			using (var connection = this.ConnectionFactory.GetConnection())
			{
				foreach(var row in rows)
				{
					await connection.ExecuteAsync(updateQuery, this.GetPlainColumnUpdateParameters(encryptedColumns, primaryKey, row));
				}
			}
		}

		private DynamicParameters GetPlainColumnUpdateParameters(IEnumerable<EncryptedColumn> encryptedColumns, IEnumerable<PrimaryKeyColumn> primaryKey, IDictionary<string, object> row)
		{
			var parameters = new DynamicParameters();

			// Add parameters for update
			foreach(var column in encryptedColumns)
			{
				parameters.Add($"@{column.Name}", row[$"{column.Name}_Encrypted"]);
			}

			// Add parameters for primary key
			foreach(var keyColumn in primaryKey)
			{
				parameters.Add($"@{keyColumn.Column}", row[keyColumn.Column]);
			}

			return parameters;
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
				throw new InvalidOperationException($"The table {table.First().Schema}.{table.Key} has no primary key. Decrypting data in tables without a primary key is not currently supported.");
			}

		}
	}
}
