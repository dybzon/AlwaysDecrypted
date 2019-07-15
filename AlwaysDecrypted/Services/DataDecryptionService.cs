namespace AlwaysDecrypted.Services
{
    using AlwaysDecrypted.Data;
    using AlwaysDecrypted.Logging;
    using AlwaysDecrypted.Models;
    using AlwaysDecrypted.Settings;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

	public class DataDecryptionService : IDataDecryptionService
	{
		public DataDecryptionService(IColumnEncryptionRepository columnEncryptionRepository, ILogger logger, ISettings settings)
		{
			ColumnEncryptionRepository = columnEncryptionRepository;
			Logger = logger;
			Settings = settings;
		}

		private IColumnEncryptionRepository ColumnEncryptionRepository { get; }
		private ILogger Logger { get; }
		private ISettings Settings { get; }

		public async Task Decrypt()
		{
			var tables = await this.GetTablesForDecryption();
			this.Logger.Log($"Found encrypted columns in the following tables {string.Join(", ", tables.Select(t => t.FullName))}", LogEventLevel.Information);
			await this.DecryptTables(tables);
		}

		public async Task<IEnumerable<Table>> GetTablesForDecryption()
		{
			return await this.ColumnEncryptionRepository.GetEncryptedTables(this.Settings.TablesToDecrypt);
		}

		public async Task DecryptTables(IEnumerable<Table> tables)
		{
			await Task.WhenAll(tables.Select(async t => await this.DecryptTable(t)));
		}

		private async Task DecryptTable(Table table)
		{
			var columns = await this.ColumnEncryptionRepository.GetEncryptedColumns(table);
			await this.PrepareColumnsForDecryption(table, columns);
			await this.ColumnEncryptionRepository.DecryptColumns(table, columns);
			await this.ColumnEncryptionRepository.CleanUpTable(columns);
		}

		private async Task PrepareColumnsForDecryption(Table table, IEnumerable<EncryptedColumn> columns)
		{
			await this.ColumnEncryptionRepository.RenameColumnsForDecryption(columns);
			await this.ColumnEncryptionRepository.CreatePlainColumns(columns);
			await this.ColumnEncryptionRepository.CreateDecryptionStatusColumn(table);
			this.Logger.Log($"Prepared columns in {table.FullName} for decryption", LogEventLevel.Information);
		}
	}
}
