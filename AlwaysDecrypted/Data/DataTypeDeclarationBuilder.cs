namespace AlwaysDecrypted.Data
{
    using AlwaysDecrypted.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;

	public class DataTypeDeclarationBuilder : IDataTypeDeclarationBuilder
	{
		public string GetColumnTypeExpression(Column column)
		{
			// Return type and (precision, scale, max) depending on data type
			if (this.DataTypesInfo.TryGetValue(column.DataType, out var dataTypeInfo))
			{
				if (!dataTypeInfo.UsesLength && !dataTypeInfo.UsesPrecision && !dataTypeInfo.UsesScale)
				{
					// If neither length, precision, or scale needs to be specified, then simply return the data type name
					return column.DataType;
				}

				// Otherwise add length and/or precision and/or scale to the expression
				var declaredMaxLength = new string[] { "nchar", "nvarchar" }.Contains(column.DataType.ToLower()) ? column.MaxLength / 2 : column.MaxLength; // Unicode character types are declared with half their actual length, because each character takes up 2 bytes of space.
				var lengthExpression = dataTypeInfo.UsesLength ? (column.MaxLength < 0 && dataTypeInfo.CanLengthBeSpecifiedAsMax) ? "MAX" : declaredMaxLength.ToString() : string.Empty;
				var precisionExpression = dataTypeInfo.UsesPrecision ? dataTypeInfo.UsesScale ? $"{column.Precision}, " : column.Precision.ToString() : string.Empty;
				var scaleExpression = dataTypeInfo.UsesScale ? column.Scale.ToString() : string.Empty;
				var nullExpression = column.IsNullable ? "NULL" : "NOT NULL";
				return $"{column.DataType}({lengthExpression}{precisionExpression}{scaleExpression}) {nullExpression}";
			}

			throw new InvalidOperationException($"Decryption of columns of type {column.DataType} is not supported");
		}

		/// <summary>
		/// Column encrypted is not supported for xml, timestamp/rowversion, image, ntext, text, sql_variant, 
		/// hierarchyid, geography, geometry, alias, user defined-types.
		///  
		/// This dictionary contains info that is relevant for building scripts for adding columns.
		/// </summary>
		private IDictionary<string, DataTypeInfo> DataTypesInfo = new Dictionary<string, DataTypeInfo>
		{
			{"numeric", new DataTypeInfo(true, true, false) },
			{"decimal", new DataTypeInfo(true, true, false) },
			{"datetimeoffset", new DataTypeInfo(true, false, false) },
			{"datetime2", new DataTypeInfo(true, false, false) },
			{"time", new DataTypeInfo(true, false, false) },
			{"char", new DataTypeInfo(false, false, true) },
			{"varchar", new DataTypeInfo(false, false, true, true) }, // Special rule for varchar(max): MaxLength will be -1, but it should be specified as MAX when creating the column.
			{"nchar", new DataTypeInfo(false, false, true) }, // Should divide length by two for unicode character types
			{"nvarchar", new DataTypeInfo(false, false, true, true) }, // Should divide length by two for unicode character types. Special rule for nvarchar(max): MaxLength will be -1, but it should be specified as MAX when creating the column.
			{"binary", new DataTypeInfo(false, false, true) },
			{"varbinary", new DataTypeInfo(false, false, true, true) }, // Special rule for varbinary(max): MaxLength will be -1, but it should be specified as MAX when creating the column.

			// Types below use neither scale, precision, or length when specified in DDL commands
			{"bigint", new DataTypeInfo(false, false, false) },
			{"bit", new DataTypeInfo(false, false, false) },
			{"smallint", new DataTypeInfo(false, false, false) },
			{"smallmoney", new DataTypeInfo(false, false, false) },
			{"int", new DataTypeInfo(false, false, false) },
			{"tinyint", new DataTypeInfo(false, false, false) },
			{"money", new DataTypeInfo(false, false, false) },
			{"float", new DataTypeInfo(false, false, false) }, // Precision is always either 24 (REAL) or 53 (FLOAT). Length is always either 4 (REAL) or 8 (FLOAT).
			{"real", new DataTypeInfo(false, false, false) },
			{"date", new DataTypeInfo(false, false, false) },
			{"smalldatetime", new DataTypeInfo(false, false, false) },
			{"datetime", new DataTypeInfo(false, false, false) },
			{"cursor", new DataTypeInfo(false, false, false) },
			{"rowversion", new DataTypeInfo(false, false, false) },
			{"uniqueidentifier", new DataTypeInfo(false, false, false) },
		};
	}
}
