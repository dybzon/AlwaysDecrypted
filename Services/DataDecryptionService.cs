﻿namespace AlwaysDecrypted.Services
{
    using AlwaysDecrypted.Data;
    using AlwaysDecrypted.Models;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

	public class DataDecryptionService : IDataDecryptionService
	{
		public DataDecryptionService(IColumnEncryptionRepository columnEncryptionRepository)
		{
			ColumnEncryptionRepository = columnEncryptionRepository;
		}

		private IColumnEncryptionRepository ColumnEncryptionRepository { get; }

		public async Task DecryptColumns()
		{
			var columns = await this.ColumnEncryptionRepository.GetEncryptedColumns();
			await this.PrepareColumnsForDecryption(columns);
			await this.ColumnEncryptionRepository.DecryptColumns(columns);
		}

		private async Task PrepareColumnsForDecryption(IEnumerable<EncryptedColumn> columns)
		{
			await this.ColumnEncryptionRepository.RenameColumnsForDecryption(columns);
			await this.ColumnEncryptionRepository.CreatePlainColumns(columns);
			await this.ColumnEncryptionRepository.CreateDecryptionStatusColumns(columns.GroupBy(c => c.Table).Select(t => (t.First().Schema, t.Key)));
		}
	}
}
