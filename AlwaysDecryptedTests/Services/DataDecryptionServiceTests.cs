namespace AlwaysDecryptedTests
{
    using AlwaysDecrypted.Data;
    using AlwaysDecrypted.Logging;
    using AlwaysDecrypted.Models;
    using AlwaysDecrypted.Services;
    using AlwaysDecrypted.Settings;
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
			var settings = A.Fake<ISettings>();
			var decryptionService = new DataDecryptionService(encryptionRepository, A.Fake<ILogger>(), settings);

			A.CallTo(() => encryptionRepository.GetEncryptedTables(settings.TablesToDecrypt))
				.Returns(new List<Table> { A.Fake<Table>() });

			await decryptionService.Decrypt();

			/* 
			 * The decryption service should:
			 * 1. Get encrypted columns
			 * 2. Prepare tables with encrypted columns for decryption
			 * 3. Perform the actual decryption
			 * 4. Clean up afterwards to remove the encrypted data and temporary columns 
			 */

			// Get encrypted tables
			A.CallTo(() => encryptionRepository.GetEncryptedTables(settings.TablesToDecrypt)).MustHaveHappened();

			// Prepare for decryption
			A.CallTo(() => encryptionRepository.RenameColumnsForDecryption(A<IEnumerable<EncryptedColumn>>._)).MustHaveHappened();
			A.CallTo(() => encryptionRepository.CreatePlainColumns(A<IEnumerable<EncryptedColumn>>._)).MustHaveHappened();
			A.CallTo(() => encryptionRepository.CreateDecryptionStatusColumn(A<Table>._)).MustHaveHappened();

			// Decrypt columns
			A.CallTo(() => encryptionRepository.DecryptColumns(A<Table>._, A<IEnumerable<EncryptedColumn>>._)).MustHaveHappened();

			// Cleanup
			A.CallTo(() => encryptionRepository.CleanUpTable(A<IEnumerable<EncryptedColumn>>._)).MustHaveHappened();
		}
	}
}
