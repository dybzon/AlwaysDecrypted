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
		/// Generates a select query for each distinct table within the given collection of columns.
		/// </summary>
		/// <param name="columns">The encrypted columns.</param>
		/// <returns>A collection of select queries.</returns>
		IEnumerable<string> GetEncryptedDataSelectQueries(IEnumerable<EncryptedColumn> columns);
	}
}
