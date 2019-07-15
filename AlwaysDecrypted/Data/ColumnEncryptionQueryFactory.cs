namespace AlwaysDecrypted.Data
{
    using System.Collections.Generic;
    using System.Linq;
    using AlwaysDecrypted.Models;

	public class ColumnEncryptionQueryFactory : IColumnEncryptionQueryFactory
	{
		public ColumnEncryptionQueryFactory(IDataTypeDeclarationBuilder dataTypeDeclarationBuilder)
		{
			this.DataTypeDeclarationBuilder = dataTypeDeclarationBuilder;
		}

		private IDataTypeDeclarationBuilder DataTypeDeclarationBuilder { get; }

		public string GetEncryptedColumnRenameQuery(Column column) 
			=> $"EXEC sp_rename '{column.FullColumnName}', '{column.Name}_Encrypted', 'COLUMN'";

		public string GetEncryptedTablesQuery(IEnumerable<Table> includedTables) => $@"SELECT 
	SCHEMA_NAME(t.schema_id) AS 'Schema', 
	OBJECT_NAME(t.object_id) AS 'Name'
FROM
	sys.tables t
	INNER JOIN sys.columns c on c.object_id = t.object_id
    INNER JOIN sys.column_encryption_keys k ON c.column_encryption_key_id = k.column_encryption_key_id
WHERE c.[encryption_type] IS NOT NULL
	AND SCHEMA_NAME(t.schema_id) + '.' + OBJECT_NAME(t.object_id) IN ({string.Join(" UNION ", includedTables.Select(t => $"SELECT '{t.Schema}.{t.Name}'"))})
GROUP BY t.schema_id, t.object_id";

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
WHERE c.[encryption_type] IS NOT NULL AND SCHEMA_NAME(t.schema_id) = @Schema AND OBJECT_NAME(t.object_id) = @Table
";

		public string GetEncryptedDataSelectQuery(IEnumerable<Column> columns, IEnumerable<Column> primaryKey) 
			=> $@"SELECT {string.Join(", ", columns.Select(c => $"{c.Name}_Encrypted"))}, {string.Join(", ", primaryKey.Select(c => c.Name))} 
				FROM {columns.First().FullTableName}
				ORDER BY {string.Join(", ", primaryKey.Select(c => c.Name))}
					OFFSET (@BatchNumber-1)*@BatchSize ROWS
					FETCH NEXT @BatchSize ROWS ONLY";

		/// We'll use default collation on plain columns for now
		public string GetPlainColumnCreateQuery(Column column)
			=> $"ALTER TABLE {column.FullTableName} ADD {column.Name} {this.DataTypeDeclarationBuilder.GetColumnTypeExpression(column)}";

		public string GetSelectPrimaryKeyColumnsQuery() => @"SELECT 
	SCHEMA_NAME(o.schema_id) AS 'Schema',
	OBJECT_NAME(i.object_id) AS 'Table',
	c.[name] AS 'Name', 
	ty.[name] AS DataType,
	c.collation_name AS Collation,
	c.max_length AS 'MaxLength',
	c.[precision] AS 'Precision',
	c.scale AS Scale,
	c.is_nullable AS IsNullable
FROM sys.indexes i 
    INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
	INNER JOIN sys.columns c ON c.column_id = ic.column_id AND c.object_id = ic.object_id
	INNER JOIN sys.types ty ON ty.user_type_id = c.user_type_id
    INNER JOIN sys.objects o ON i.object_id = o.object_ID
WHERE i.is_primary_key = 1
    AND o.[type_desc] = 'USER_TABLE'
	AND SCHEMA_NAME(o.schema_id) = @Schema
	AND OBJECT_NAME(i.object_id) = @Table";

		public string GetDecryptionStatusColumnCreateQuery(Table table)
			=> $"ALTER TABLE {table.FullName} ADD IsDataDecrypted BIT NULL";

		public string GetPlainColumnsUpdateQuery(IEnumerable<Column> encryptedColumns, IEnumerable<Column> primaryKey) 
			=> $@"UPDATE {encryptedColumns.First().FullTableName} 
				SET IsDataDecrypted = 1, {string.Join(", ", encryptedColumns.Select(c => $"{c.Name} = @{c.Name}"))} 
				WHERE {string.Join(" AND ", primaryKey.Select(c => $"{c.Name} = @{c.Name}"))}";

		public string GetCleanUpQuery(IEnumerable<Column> columns)
			=> $"ALTER TABLE {columns.First().FullTableName} DROP COLUMN IsDataDecrypted, {string.Join(", ", columns.Select(c => $"{c.Name}_Encrypted"))}";

		public string GetTempUpdateTableCreateQuery(IEnumerable<Column> columns)
			=> $"CREATE TABLE {this.GetTempUpdateTableName(columns)} " +
			$"({string.Join(", ", columns.Select(c => $"{c.Name} {this.DataTypeDeclarationBuilder.GetColumnTypeExpression(c)}"))})";

		public string GetTempUpdateTableName(IEnumerable<Column> columns, IEnumerable<Column> moreColumns)
			=> this.GetTempUpdateTableName(columns.Concat(moreColumns));

		public string GetTempUpdateTableName(IEnumerable<Column> columns)
			=> $"#Temp_{columns.First().Schema}_{columns.First().Table}";

		public string GetPlainValuesFromTempTableUpdateQuery(IEnumerable<Column> encryptedColumns, IEnumerable<Column> primaryKey)
			=> $@"UPDATE o 
				SET {string.Join(", ", encryptedColumns.Select(c => $"o.{c.Name} = t.{c.Name}"))} 
				FROM {encryptedColumns.First().FullTableName} o
					INNER JOIN {this.GetTempUpdateTableName(encryptedColumns, primaryKey)} t
						ON {string.Join(" AND ", primaryKey.Select(c => $"o.{c.Name} = t.{c.Name}"))}";

		public string GetTempUpdateTableCreateQuery(IEnumerable<Column> columns, IEnumerable<Column> moreColumns)
			=> this.GetTempUpdateTableCreateQuery(columns.Concat(moreColumns));
	}
}
