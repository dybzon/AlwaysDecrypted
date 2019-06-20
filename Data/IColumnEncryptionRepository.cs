namespace AlwaysDecrypted.Data
{
	using AlwaysDecrypted.Models;
	using System.Collections.Generic;
    using System.Data;
    using System.Threading.Tasks;

    public interface IColumnEncryptionRepository
	{
		Task<IEnumerable<EncryptedColumn>> GetEncryptedColumns();

		Task DecryptColumns(IEnumerable<EncryptedColumn> columns);

		Task RenameColumnsForDecryption(IEnumerable<EncryptedColumn> columns);

		Task CreatePlainColumns(IEnumerable<EncryptedColumn> columns);
	}
}
