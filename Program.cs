namespace AlwaysDecrypted
{
    using AlwaysDecrypted.Data;
    using AlwaysDecrypted.Setup;
    using Autofac;
    using System;
    using System.Threading.Tasks;

    public class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("sup");

			var container = DependencyBuilder.Build();
			var scope = container.BeginLifetimeScope();
			var columnEncryptionRepository = scope.Resolve<IColumnEncryptionRepository>();

			var columns = await columnEncryptionRepository.GetEncryptedColumns();


			foreach(var column in columns)
			{
				Console.WriteLine($"{column.Schema}.{column.Table}.{column.Name}");
			}
		}
    }
}
