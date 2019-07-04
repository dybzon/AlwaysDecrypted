namespace AlwaysDecrypted.Data
{
	using System.Data;
	using System.Data.SqlClient;

	public class ConnectionFactory : IConnectionFactory
	{
		private IConnectionStringBuilder ConnectionStringBuilder { get; }

		public ConnectionFactory(IConnectionStringBuilder connectionStringBuilder)
		{
			ConnectionStringBuilder = connectionStringBuilder;
		}

		public IDbConnection GetConnection()
		{
			return this.GetSqlConnection();
		}

		public SqlConnection GetSqlConnection()
		{
			return new SqlConnection(this.ConnectionStringBuilder.Build());
		}
	}
}
