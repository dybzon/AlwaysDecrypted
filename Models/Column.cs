namespace AlwaysDecrypted.Models
{
	public class Column
	{
		public string Name { get; set; }

		public string Table { get; set; }

		public string Schema { get; set; }

		public string DataType { get; set; }

		public string Collation { get; set; }

		public int MaxLength { get; set; }

		public int Precision { get; set; }

		public int Scale { get; set; }

		public bool IsNullable { get; set; }

		public string FullTableName => $"{Schema}.{Table}";

		public string FullColumnName => $"{FullTableName}.{Name}";
	}
}
