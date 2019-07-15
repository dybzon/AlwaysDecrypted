namespace AlwaysDecrypted.Data
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using AlwaysDecrypted.Models;

	public class PrimaryKeyValidationService : IPrimaryKeyValidationService
	{
		public void ValidatePrimaryKeyColumns(Table table, IEnumerable<Column> primaryKeyColumns)
		{
			// Throw if a table with encrypted columns has no primary key.
			if (!primaryKeyColumns.Any())
			{
				throw new InvalidOperationException($"The table {table.FullName} has no primary key. Decrypting data in tables without a primary key is not currently supported.");
			}
		}
	}
}
