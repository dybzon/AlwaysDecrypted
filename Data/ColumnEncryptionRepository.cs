namespace AlwaysDecrypted.Data
{
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Linq;
    using System.Threading.Tasks;
	using AlwaysDecrypted.Models;
    using Dapper;

    public class ColumnEncryptionRepository : IColumnEncryptionRepository
	{
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

			// Throw if a table has no primary key
			this.ValidatePrimaryKeyColumns(tableGroups, primaryKeyColumns);

			// Select primary key and encrypted data for all records, batched in groups of 10k.
			var test = tableGroups.First();
			await this.DecryptDataForTable(test, primaryKeyColumns.Where(c => c.Table.Equals(test.Key, StringComparison.InvariantCultureIgnoreCase)));

			// Update the plain columns, one batch at a time. Set the IsDataDecrypted column to 1 at the same time.
			// Remove the IsDataDecrypted column and the _Encrypted columns when the last batch finishes decrypting.
		}

		private async Task DecryptDataForTable(IEnumerable<EncryptedColumn> encryptedColumns, IEnumerable<PrimaryKeyColumn> primaryKey)
		{
			using (var connection = this.ConnectionFactory.GetConnection())
			{
				var rows = (await connection.QueryAsync(this.QueryFactory.GetEncryptedDataSelectQuery(encryptedColumns, primaryKey)))
					.Select(row => (IDictionary<string, object>)row); // The dynamic returned by Dapper is always a DapperRow, which is also a dictionary of <string, object>.

				await this.UpdatePlainColumns(encryptedColumns, primaryKey, rows);
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
				foreach(var column in columns)
				{
					await connection.ExecuteAsync(this.QueryFactory.GetEncryptedColumnRenameQuery(column));
				}
			}
		}

		public async Task CreatePlainColumns(IEnumerable<EncryptedColumn> columns)
		{
			using (var connection = this.ConnectionFactory.GetConnection())
			{
				foreach(var column in columns)
				{
					await connection.ExecuteAsync(this.QueryFactory.GetPlainColumnCreateQuery(column));
				}
			}
		}

		public async Task CreateDecryptionStatusColumns(IEnumerable<(string, string)> tables)
		{
			foreach(var table in tables)
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
		/// Gets primary key columns for a given table on a given schema.
		/// </summary>
		/// <param name="schemaName">The name of the schema.</param>
		/// <param name="tableName">The name of the table.</param>
		/// <returns></returns>
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
			var tablesWithoutPrimaryKey = tableGroups.Where(table => !primaryKeyColumns.Any(col => col.Table.Equals(table.Key, System.StringComparison.InvariantCultureIgnoreCase)));
			if (tablesWithoutPrimaryKey.Any())
			{
				var table = tablesWithoutPrimaryKey.First();
				throw new InvalidOperationException($"The table {table.First().Schema}.{table.Key} has no primary key. Decrypting data in tables without a primary key is not currently supported.");
			}

		}
	}
}
