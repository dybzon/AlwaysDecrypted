namespace AlwaysDecrypted.Models
{
	public struct DataTypeInfo
	{
		public DataTypeInfo(bool usesScale, bool usesPrecision, bool usesLength)
		{
			this.UsesScale = usesScale;
			this.UsesPrecision = usesPrecision;
			this.UsesLength = usesLength;
		}

		public bool UsesScale { get; }
		public bool UsesPrecision { get; }
		public bool UsesLength { get; }
	}
}
