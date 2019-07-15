namespace AlwaysDecrypted.Data
{
	using AlwaysDecrypted.Models;
	using System.Collections.Generic;

	/// <summary>
	/// A service for validating primary keys.
	/// 
	/// All tables with encrypted columns must have a primary key. 
	/// Otherwise we cannot update the tables correctly with decrypted data.
	/// </summary>
	public interface IPrimaryKeyValidationService
	{
		void ValidatePrimaryKeyColumns(Table table, IEnumerable<Column> primaryKeyColumns);
	}
}
