namespace AlwaysDecrypted.Data
{
	using AlwaysDecrypted.Settings;

	public class ConnectionStringBuilder : IConnectionStringBuilder
	{
		public ConnectionStringBuilder(ISettings settings)
		{
			Settings = settings;
		}

		private ISettings Settings { get; }

		public string Build()
		{
			return this.Settings.ConnectionString;
		}
	}
}
