namespace AlwaysDecrypted.Models
{
	public class Table
	{
		public Table()
		{
		}

		public Table(string fullTableName)
		{
			var tableParts = fullTableName.Trim().Split('.');

			if(tableParts.Length > 1)
			{
				this.Schema = tableParts[0];
				this.Name = tableParts[1];
			}
			else
			{
				// Assume the schema is dbo if nothing else is specified
				this.Schema = "dbo";
				this.Name = tableParts[0];
			}
		}
		public string Schema { get; set; }

		public string Name { get; set; }

		public string FullName => $"{this.Schema}.{this.Name}";
	}
}
