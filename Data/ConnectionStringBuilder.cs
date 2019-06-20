namespace AlwaysDecrypted.Data
{
	public class ConnectionStringBuilder : IConnectionStringBuilder
	{
		public string Build()
		{
			return "Data Source=.;Initial Catalog=AlwaysDecrypted;Integrated Security=SSPI;Column Encryption Setting=enabled;";
		}
	}
}
