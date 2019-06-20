namespace AlwaysDecrypted.Models
{
	using System.Data;

	/// <summary>
	/// A simple data model that represents an encrypted column in a sql database table.
	/// </summary>
	public class EncryptedColumn
	{
		public string Name { get; set; }

		public string Table { get; set; }

		public string Schema { get; set; }

		public SqlDbType DataType { get; set; }

		public string ColumnKey { get; set; }

		public string EncryptionType { get; set; }

		public string EncryptionAlgorithm { get; set; }

		public string Collation { get; set; }

		public int MaxLength { get; set; }

		public int Precision { get; set; }

		public int Scale { get; set; }

		public bool IsNullable { get; set; }
	}
}
