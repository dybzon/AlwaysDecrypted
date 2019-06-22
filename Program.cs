namespace AlwaysDecrypted
{
    using AlwaysDecrypted.Services;
    using AlwaysDecrypted.Setup;
    using Autofac;
    using System;
    using System.Threading.Tasks;

    public class Program
    {
        static async Task Main(string[] args)
        {
			// Build dependencies
			var container = DependencyBuilder.Build();
			var scope = container.BeginLifetimeScope();

			// Get decryption service and decrypt everything
			var decryptionService = scope.Resolve<IDataDecryptionService>();

			Console.WriteLine("Decryption has begun...");
			await decryptionService.DecryptColumns();

			Console.WriteLine($"Everything was decrypted...");
		}
    }
}
