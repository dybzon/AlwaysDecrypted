namespace AlwaysDecrypted
{
    using AlwaysDecrypted.Data;
    using AlwaysDecrypted.Services;
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
			var decryptionService = scope.Resolve<IDataDecryptionService>();

			await decryptionService.DecryptColumns();

			Console.WriteLine($"Done .. apparently...");
		}
    }
}
