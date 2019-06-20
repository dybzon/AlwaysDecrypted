namespace AlwaysDecrypted.Data
{
	using System.Collections.Generic;
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
			var primaryKeyColumns = await Task.WhenAll(tableGroups.Select(async table => await this.GetPrimaryKeyColumns(table.First().Schema, table.Key)));

			// If no primary key, throw exception.
			// Compare primaryKeyColumns.table to encryptedColumns.table to determine this...

			// Select primary key and encrypted data for all records, batched in groups of 10k.
			// Update the plain columns, one batch at a time. Set the IsDataDecrypted column to 1 at the same time.
			// Remove the IsDataDecrypted column when the last batch finishes decrypting.
			using(var connection = this.ConnectionFactory.GetConnection())
			{
			}

			throw new System.NotImplementedException();
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
				var tasks = new List<Task>();
				foreach(var column in columns)
				{
					tasks.Add(connection.ExecuteAsync(this.QueryFactory.GetEncryptedColumnRenameQuery(column)));
				}

				await Task.WhenAll(tasks.ToArray());
			}
		}

		public async Task CreatePlainColumns(IEnumerable<EncryptedColumn> columns)
		{
			using (var connection = this.ConnectionFactory.GetConnection())
			{
				var tasks = new List<Task>();
				foreach(var column in columns)
				{
					tasks.Add(connection.ExecuteAsync(this.QueryFactory.GetPlainColumnCreateQuery(column)));
				}

				await Task.WhenAll(tasks.ToArray());
			}
		}

		private async Task<IEnumerable<PrimaryKeyColumn>> GetPrimaryKeyColumns(string schemaName, string tableName)
		{
			using(var connection = this.ConnectionFactory.GetConnection())
			{
				return await connection.QueryAsync<PrimaryKeyColumn>(this.QueryFactory.GetSelectPrimaryKeyColumnsQuery(), new { Schema = schemaName, Table = tableName });
			}
		}
	}
}
