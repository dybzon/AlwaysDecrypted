namespace AlwaysDecrypted.Data
{
	using AlwaysDecrypted.Models;

	public interface IDataTypeDeclarationBuilder
	{
		string GetColumnTypeExpression(Column column);
	}
}
