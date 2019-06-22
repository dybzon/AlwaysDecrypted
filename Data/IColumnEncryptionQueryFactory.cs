namespace AlwaysDecrypted.Data
{
	using AlwaysDecrypted.Models;
    using System.Collections.Generic;

    public interface IColumnEncryptionQueryFactory
	{
		string GetEncryptedColumnsSelectQuery();

		string GetEncryptedColumnRenameQuery(EncryptedColumn column);

		string GetPlainColumnCreateQuery(EncryptedColumn column);

		/// <summary>
		/// Generates a select query for a table with the given collection of columns.
		/// </summary>
		/// <param name="columns">The encrypted columns.</param>
		/// <param name="primaryKey">The primary key columns.</param>
		/// <returns>A select query for the table containing the given columns.</returns>
		string GetEncryptedDataSelectQuery(IEnumerable<EncryptedColumn> columns, IEnumerable<PrimaryKeyColumn> primaryKey);

		string GetSelectPrimaryKeyColumnsQuery();

		string GetDecryptionStatusColumnCreateQuery(string schemaName, string tableName);

		string GetPlainColumnsUpdateQuery(IEnumerable<EncryptedColumn> encryptedColumns, IEnumerable<PrimaryKeyColumn> primaryKey);

		string GetCleanUpQuery(IEnumerable<EncryptedColumn> columns);
	}
}
