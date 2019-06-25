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
			try
			{
				Setup();
				await Run();
			}
			catch(Exception e)
			{
				Console.WriteLine(e.Message);
			}
		}

		private static void Setup()
		{
			// Build dependency container
			DependencyBuilder.Build().BeginLifetimeScope();
		}

		private static async Task Run()
		{
			// Get decryption service and decrypt everything
			var decryptionService = DependencyBuilder.Container.Resolve<IDataDecryptionService>();

			Console.WriteLine("Decryption has begun...");
			await decryptionService.DecryptColumns();
			Console.WriteLine($"Everything was decrypted...");
		}
	}
}
