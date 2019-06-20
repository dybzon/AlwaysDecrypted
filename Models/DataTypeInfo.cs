namespace AlwaysDecrypted.Models
{
	public struct DataTypeInfo
	{
		public DataTypeInfo(bool usesScale, bool usesPrecision, bool usesLength)
		{
			this.UsesScale = usesScale;
			this.UsesPrecision = usesPrecision;
			this.UsesLength = usesLength;
			this.CanLengthBeSpecifiedAsMax = false;
		}

		public DataTypeInfo(bool usesScale, bool usesPrecision, bool usesLength, bool canLengthBeSpecifiedAsMax)
		{
			this.UsesScale = usesScale;
			this.UsesPrecision = usesPrecision;
			this.UsesLength = usesLength;
			this.CanLengthBeSpecifiedAsMax = canLengthBeSpecifiedAsMax;
		}

		public bool UsesScale { get; }
		public bool UsesPrecision { get; }
		public bool UsesLength { get; }
		public bool CanLengthBeSpecifiedAsMax { get; }
	}
}
