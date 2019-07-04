namespace AlwaysDecryptedTests
{
    using AlwaysDecrypted.Data;
    using AlwaysDecrypted.Logging;
    using AlwaysDecrypted.Models;
    using AlwaysDecrypted.Services;
    using FakeItEasy;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Xunit;

	public class DataDecryptionServiceTests
	{
		[Fact]
		public async Task DecryptColumnsShouldCallRepository()
		{
			var encryptionRepository = A.Fake<IColumnEncryptionRepository>();
			var decryptionService = new DataDecryptionService(encryptionRepository, A.Fake<ILogger>());
			await decryptionService.DecryptColumns();

			/* 
			 * The decryption service should:
			 * 1. Get encrypted columns
			 * 2. Prepare tables with encrypted columns for decryption
			 * 3. Perform the actual decryption
			 * 4. Clean up afterwards to remove the encrypted data and temporary columns 
			 */

			// Get encrypted columns
			A.CallTo(() => encryptionRepository.GetEncryptedColumns()).MustHaveHappened();

			// Prepare for decryption
			A.CallTo(() => encryptionRepository.RenameColumnsForDecryption(A<IEnumerable<EncryptedColumn>>._)).MustHaveHappened();
			A.CallTo(() => encryptionRepository.CreatePlainColumns(A<IEnumerable<EncryptedColumn>>._)).MustHaveHappened();
			A.CallTo(() => encryptionRepository.CreateDecryptionStatusColumns(A<IEnumerable<(string, string)>>._)).MustHaveHappened();

			// Decrypt
			A.CallTo(() => encryptionRepository.DecryptColumns(A<IEnumerable<EncryptedColumn>>._)).MustHaveHappened();

			// Cleanup
			A.CallTo(() => encryptionRepository.CleanUpTables(A<IEnumerable<EncryptedColumn>>._)).MustHaveHappened();
		}
	}
}
