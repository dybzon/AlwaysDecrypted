namespace AlwaysDecrypted.Settings
{
	using System.Collections.Generic;

	public interface ISettings
	{
		string ConnectionString { get; }
		string Database { get; set; }
		string Server { get; set; }
		IEnumerable<string> TablesToDecrypt { get; set; }
	}
}
