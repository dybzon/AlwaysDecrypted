namespace AlwaysDecrypted.Data
{
	using System.Data;
    using System.Data.SqlClient;

    public interface IConnectionFactory
	{
		IDbConnection GetConnection();

		SqlConnection GetSqlConnection();
	}
}
