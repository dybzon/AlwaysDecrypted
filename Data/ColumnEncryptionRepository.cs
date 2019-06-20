namespace AlwaysDecrypted.Data
{
	using System.Collections.Generic;
    using System.Data;
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
			// Try out the Z.BulkOperations package for this (referred to be dapper-tutorial.net).
			// They should have BulkUpdate support which could be nice to use for writing decrypted values to the db.
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
	}
}
