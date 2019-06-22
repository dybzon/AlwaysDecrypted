namespace AlwaysDecrypted.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using AlwaysDecrypted.Models;

	public class ColumnEncryptionQueryFactory : IColumnEncryptionQueryFactory
	{
		private const int BatchSize = 10000;

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

		public string GetEncryptedDataSelectQuery(IEnumerable<EncryptedColumn> columns, IEnumerable<PrimaryKeyColumn> primaryKey) => $@"SELECT TOP {BatchSize} {string.Join(", ", columns.Select(c => $"{c.Name}_Encrypted"))}, {string.Join(", ", primaryKey.Select(c => c.Column))} 
				FROM {columns.First().Schema}.{columns.First().Table}
				WHERE IsDataDecrypted IS NULL";
			

		/// We'll use default collation on plain columns for now
		public string GetPlainColumnCreateQuery(EncryptedColumn column)
			=> $"ALTER TABLE {column.Schema}.{column.Table} ADD {column.Name} {this.GetColumnTypeExpression(column)} {(column.IsNullable ? "NULL" : "NOT NULL")}";

		public string GetSelectPrimaryKeyColumnsQuery() => @"SELECT 
	SCHEMA_NAME(o.schema_id) AS 'Schema',
	OBJECT_NAME(i.object_id) AS 'Table',
	c.[name] AS 'Column'
FROM sys.indexes i 
    INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
	INNER JOIN sys.columns c ON c.column_id = ic.column_id AND c.object_id = ic.object_id
    INNER JOIN sys.objects o ON i.object_id = o.object_ID
WHERE i.is_primary_key = 1
    AND o.[type_desc] = 'USER_TABLE'
	AND SCHEMA_NAME(o.schema_id) = @Schema
	AND OBJECT_NAME(i.object_id) = @Table";

		private string GetColumnTypeExpression(EncryptedColumn column)
		{
			// Return type and (precision, scale, max) depending on data type
			if(this.DataTypesInfo.TryGetValue(column.DataType, out var dataTypeInfo))
			{
				if(!dataTypeInfo.UsesLength && !dataTypeInfo.UsesPrecision && !dataTypeInfo.UsesScale)
				{
					// If neither length, precision, or scale needs to be specified, then simply return the data type name
					return column.DataType;
				}

				// Otherwise add length and/or precision and/or scale to the expression
				var lengthExpression = dataTypeInfo.UsesLength ? (column.MaxLength < 0 && dataTypeInfo.CanLengthBeSpecifiedAsMax) ? "MAX" : column.MaxLength.ToString() : string.Empty;
				var precisionExpression = dataTypeInfo.UsesPrecision ? dataTypeInfo.UsesScale ? $"{column.Precision}, " : column.Precision.ToString() : string.Empty;
				var scaleExpression = dataTypeInfo.UsesScale ? column.Scale.ToString() : string.Empty;
				return $"{column.DataType}({lengthExpression}{precisionExpression}{scaleExpression})";
			}

			throw new InvalidOperationException($"Decryption of columns of type {column.DataType} is not supported");
		}

		public string GetDecryptionStatusColumnCreateQuery(string schemaName, string tableName)
		{
			return $"ALTER TABLE {schemaName}.{tableName} ADD IsDataDecrypted BIT NULL";
		}

		public string GetPlainColumnsUpdateQuery(IEnumerable<EncryptedColumn> encryptedColumns, IEnumerable<PrimaryKeyColumn> primaryKey) 
			=> $@"UPDATE {encryptedColumns.First().Schema}.{encryptedColumns.First().Table} 
				SET IsDataDecrypted = 1, {string.Join(", ", encryptedColumns.Select(c => $"{c.Name} = @{c.Name}"))} 
				WHERE {string.Join(" AND ", primaryKey.Select(c => $"{c.Column} = @{c.Column}"))}";

		/// <summary>
		/// Column encrypted is not supported for xml, timestamp/rowversion, image, ntext, text, sql_variant, 
		/// hierarchyid, geography, geometry, alias, user defined-types.
		///  
		/// This dictionary contains info that is relevant for building scripts for adding columns.
		/// </summary>
		private IDictionary<string, DataTypeInfo> DataTypesInfo = new Dictionary<string, DataTypeInfo>
		{
			{"numeric", new DataTypeInfo(true, true, false) },
			{"decimal", new DataTypeInfo(true, true, false) },
			{"datetimeoffset", new DataTypeInfo(true, false, false) },
			{"datetime2", new DataTypeInfo(true, false, false) },
			{"time", new DataTypeInfo(true, false, false) },
			{"char", new DataTypeInfo(false, false, true) }, 
			{"varchar", new DataTypeInfo(false, false, true, true) }, // Special rule for varchar(max): MaxLength will be -1, but it should be specified as MAX when creating the column.
			{"nchar", new DataTypeInfo(false, false, true) }, // Should divide length by two for unicode character types
			{"nvarchar", new DataTypeInfo(false, false, true, true) }, // Should divide length by two for unicode character types. Special rule for nvarchar(max): MaxLength will be -1, but it should be specified as MAX when creating the column.
			{"binary", new DataTypeInfo(false, false, true) },
			{"varbinary", new DataTypeInfo(false, false, true, true) }, // Special rule for varbinary(max): MaxLength will be -1, but it should be specified as MAX when creating the column.

			// Types below use neither scale, precision, or length when specified in DDL commands
			{"bigint", new DataTypeInfo(false, false, false) },
			{"bit", new DataTypeInfo(false, false, false) },
			{"smallint", new DataTypeInfo(false, false, false) },
			{"smallmoney", new DataTypeInfo(false, false, false) },
			{"int", new DataTypeInfo(false, false, false) },
			{"tinyint", new DataTypeInfo(false, false, false) },
			{"money", new DataTypeInfo(false, false, false) },
			{"float", new DataTypeInfo(false, false, false) }, // Precision is always either 24 (REAL) or 53 (FLOAT). Length is always either 4 (REAL) or 8 (FLOAT).
			{"real", new DataTypeInfo(false, false, false) },
			{ "date", new DataTypeInfo(false, false, false) },
			{ "smalldatetime", new DataTypeInfo(false, false, false) },
			{"datetime", new DataTypeInfo(false, false, false) },
			{ "cursor", new DataTypeInfo(false, false, false) },
			{"rowversion", new DataTypeInfo(false, false, false) },
			{"uniqueidentifier", new DataTypeInfo(false, false, false) },
		};
	}
}
