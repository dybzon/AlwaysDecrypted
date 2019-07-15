namespace AlwaysDecrypted.Settings
{
    using AlwaysDecrypted.Models;
    using System.Collections.Generic;

	public class Settings : ISettings
	{
		public Settings()
		{
		}

		public string ConnectionString => $"Data Source={this.Server};Initial Catalog={this.Database};Integrated Security=SSPI;Column Encryption Setting=enabled;";
		public string Database { get; set; }
		public string Server { get; set; } = ".";
		public IEnumerable<Table> TablesToDecrypt { get; set; }
	}
}
