namespace AlwaysDecrypted.Models
{
	/// <summary>
	/// A simple data model that represents an encrypted column in a sql database table.
	/// </summary>
	public class EncryptedColumn : Column
	{
		public string ColumnKey { get; set; }

		public string EncryptionType { get; set; }

		public string EncryptionAlgorithm { get; set; }
	}
}
