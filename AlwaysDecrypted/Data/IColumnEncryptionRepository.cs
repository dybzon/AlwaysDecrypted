namespace AlwaysDecrypted.Data
{
	using AlwaysDecrypted.Models;
	using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IColumnEncryptionRepository
	{
		Task<IEnumerable<EncryptedColumn>> GetEncryptedColumns();

		Task<IEnumerable<EncryptedColumn>> GetEncryptedColumns(Table table);

		Task<IEnumerable<Table>> GetEncryptedTables(IEnumerable<Table> includedTables);

		Task DecryptColumns(Table table, IEnumerable<EncryptedColumn> columns);

		Task CleanUpTable(IEnumerable<EncryptedColumn> columns);

		Task RenameColumnsForDecryption(IEnumerable<EncryptedColumn> columns);

		Task CreatePlainColumns(IEnumerable<EncryptedColumn> columns);

		Task CreateDecryptionStatusColumn(Table table);
	}
}
