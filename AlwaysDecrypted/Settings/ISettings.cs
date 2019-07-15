namespace AlwaysDecrypted.Settings
{
    using AlwaysDecrypted.Models;
    using System.Collections.Generic;

	public interface ISettings
	{
		string ConnectionString { get; }
		string Database { get; set; }
		string Server { get; set; }
		IEnumerable<Table> TablesToDecrypt { get; set; }
	}
}
