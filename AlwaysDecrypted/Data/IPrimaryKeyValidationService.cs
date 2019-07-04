namespace AlwaysDecrypted.Data
{
	using AlwaysDecrypted.Models;
	using System.Collections.Generic;
	using System.Linq;

	/// <summary>
	/// A service for validating primary keys.
	/// 
	/// All tables with encrypted columns must have a primary key. 
	/// Otherwise we cannot update the tables correctly with decrypted data.
	/// </summary>
	public interface IPrimaryKeyValidationService
	{
		void ValidatePrimaryKeyColumns(IEnumerable<string> tables, IEnumerable<Column> primaryKeyColumns);
		void ValidatePrimaryKeyColumns(IEnumerable<IGrouping<string, Column>> tableGroups, IEnumerable<Column> primaryKeyColumns);
	}
}
