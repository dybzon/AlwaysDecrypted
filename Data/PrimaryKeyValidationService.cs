namespace AlwaysDecrypted.Data
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using AlwaysDecrypted.Models;

	public class PrimaryKeyValidationService : IPrimaryKeyValidationService
	{
		public void ValidatePrimaryKeyColumns(IEnumerable<string> tables, IEnumerable<Column> primaryKeyColumns)
		{
			var tablesWithoutPrimaryKey = tables.Where(t => !primaryKeyColumns.Any(c => c.FullTableName.Equals(t, StringComparison.InvariantCultureIgnoreCase)));

			// Throw if a table with encrypted columns has no primary key.
			if (tablesWithoutPrimaryKey.Any())
			{
				throw new InvalidOperationException($"The tables {string.Join(", ", tablesWithoutPrimaryKey)} have no primary key. Decrypting data in tables without a primary key is not currently supported.");
			}
		}

		public void ValidatePrimaryKeyColumns(IEnumerable<IGrouping<string, Column>> tableGroups, IEnumerable<Column> primaryKeyColumns)
		{
			this.ValidatePrimaryKeyColumns(tableGroups.Select(t => t.Key), primaryKeyColumns);
		}
	}
}
