using AlwaysDecrypted.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlwaysDecrypted.Services
{
	public interface IDataDecryptionService
	{
		Task<IEnumerable<Table>> GetTablesForDecryption();
		Task DecryptTables(IEnumerable<Table> tables);
		Task Decrypt();
	}
}
