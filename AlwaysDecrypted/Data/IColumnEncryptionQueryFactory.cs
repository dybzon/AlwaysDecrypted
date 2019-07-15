namespace AlwaysDecrypted.Data
{
	using AlwaysDecrypted.Models;
    using System.Collections.Generic;

    public interface IColumnEncryptionQueryFactory
	{
		string GetEncryptedColumnsSelectQuery();

		string GetEncryptedColumnRenameQuery(Column column);

		string GetEncryptedTablesQuery(IEnumerable<Table> includedTables);

		string GetPlainColumnCreateQuery(Column column);

		/// <summary>
		/// Generates a select query for a table with the given collection of columns.
		/// </summary>
		/// <param name="columns">The encrypted columns.</param>
		/// <param name="primaryKey">The primary key columns.</param>
		/// <returns>A select query for the table containing the given columns.</returns>
		string GetEncryptedDataSelectQuery(IEnumerable<Column> columns, IEnumerable<Column> primaryKey);

		string GetSelectPrimaryKeyColumnsQuery();

		string GetDecryptionStatusColumnCreateQuery(Table table);

		string GetPlainColumnsUpdateQuery(IEnumerable<Column> encryptedColumns, IEnumerable<Column> primaryKey);

		string GetCleanUpQuery(IEnumerable<Column> columns);

		string GetTempUpdateTableCreateQuery(IEnumerable<Column> columns, IEnumerable<Column> moreColumns);

		string GetTempUpdateTableCreateQuery(IEnumerable<Column> columns);

		string GetTempUpdateTableName(IEnumerable<Column> columns, IEnumerable<Column> moreColumns);

		string GetTempUpdateTableName(IEnumerable<Column> columns);

		string GetPlainValuesFromTempTableUpdateQuery(IEnumerable<Column> encryptedColumns, IEnumerable<Column> primaryKey);
	}
}
