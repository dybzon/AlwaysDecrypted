namespace AlwaysDecrypted.Data
{
	using System.Collections.Generic;
    using System.Linq;
    using AlwaysDecrypted.Models;

	public class ColumnEncryptionQueryFactory : IColumnEncryptionQueryFactory
	{
		public string GetEncryptedColumnRenameQuery(EncryptedColumn column) 
			=> $"EXEC sp_rename '{column.Schema}.{column.Table}.{column.Name}', '{column.Name}_Encrypted', 'COLUMN'";

		public string GetEncryptedColumnsSelectQuery() => @"SELECT 
	SCHEMA_NAME(t.schema_id) AS 'Schema', 
	OBJECT_NAME(t.object_id) AS 'Table', 
	c.[name] AS 'Name', 
	ty.[name] AS DataType,
	k.[name] AS ColumnKey,
	c.encryption_type_desc AS EncryptionType,
	c.encryption_algorithm_name AS EncryptionAlgorithm,
	c.collation_name AS Collation,
	c.max_length AS 'MaxLength',
	c.[precision] AS 'Precision',
	c.scale AS Scale,
	c.is_nullable AS IsNullable
FROM
	sys.tables t
	INNER JOIN sys.columns c on c.object_id = t.object_id
	INNER JOIN sys.types ty ON ty.user_type_id = c.user_type_id
    INNER JOIN sys.column_encryption_keys k ON c.column_encryption_key_id = k.column_encryption_key_id
WHERE c.[encryption_type] IS NOT NULL
";

		public IEnumerable<string> GetEncryptedDataSelectQueries(IEnumerable<EncryptedColumn> columns)
			=> columns.GroupBy(c => c.Table).Select(t => $"SELECT {string.Join(", ", t)} FROM {t.Key}");

		public string GetPlainColumnCreateQuery(EncryptedColumn column)
			=> $"ALTER TABLE {column.Schema}.{column.Table} ADD {column.Name} {this.GetColumnType(column)} {(column.IsNullable ? "NULL" : "NOT NULL")}";

		private string GetColumnType(EncryptedColumn column)
		{
			// Check for data type category
			// https://docs.microsoft.com/en-us/sql/t-sql/data-types/data-types-transact-sql?view=sql-server-2017
			// Return type and (precision, scale, max) depending on data type
			return "";
		}

		/// <summary>
		/// Column encrypted is not supported for xml, timestamp/rowversion, image, ntext, text, sql_variant, 
		/// hierarchyid, geography, geometry, alias, user defined-types.
		///  
		/// This dictionary contains info that is relevant for building scripts for adding columns.
		/// </summary>
		private IDictionary<string, DataTypeInfo> DataTypesInfo = new Dictionary<string, DataTypeInfo>
		{
			{"bigint", new DataTypeInfo(false, false, false) },
			{"numeric", new DataTypeInfo(true, true, false) },
			{"bit", new DataTypeInfo(false, false, false) },
			{"smallint", new DataTypeInfo(false, false, false) },
			{"decimal", new DataTypeInfo(true, true, false) },
			{"smallmoney", new DataTypeInfo(false, false, false) },
			{"int", new DataTypeInfo(false, false, false) },
			{"tinyint", new DataTypeInfo(false, false, false) },
			{"money", new DataTypeInfo(false, false, false) },
			{"float", new DataTypeInfo(false, false, false) }, // Precision is always either 24 (REAL) or 53 (FLOAT). Length is always either 4 (REAL) or 8 (FLOAT).
			{"real", new DataTypeInfo(false, false, false) },

			{ "date", new DataTypeInfo(false, false, false) },
			{"datetimeoffset", new DataTypeInfo(false, false, false) },
			{"datetime2", new DataTypeInfo(false, false, false) },
			{"smalldatetime", new DataTypeInfo(false, false, false) },
			{"datetime", new DataTypeInfo(false, false, false) },
			{"time", new DataTypeInfo(false, false, false) },
			{"char", new DataTypeInfo(false, false, false) },
			{"varchar", new DataTypeInfo(false, false, false) },
			{"nchar", new DataTypeInfo(false, false, false) },
			{"nvarchar", new DataTypeInfo(false, false, false) },
			{"binary", new DataTypeInfo(false, false, false) },
			{"varbinary", new DataTypeInfo(false, false, false) },
			{"cursor", new DataTypeInfo(false, false, false) },
			{"rowversion", new DataTypeInfo(false, false, false) },
			{"uniqueidentifier", new DataTypeInfo(false, false, false) },
		};
	}
}
