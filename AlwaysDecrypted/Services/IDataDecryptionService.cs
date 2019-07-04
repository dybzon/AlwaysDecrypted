using System.Threading.Tasks;

namespace AlwaysDecrypted.Services
{
	public interface IDataDecryptionService
	{
		Task DecryptColumns();
	}
}
