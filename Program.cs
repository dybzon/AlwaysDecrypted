namespace AlwaysDecrypted
{
    using AlwaysDecrypted.Logging;
    using AlwaysDecrypted.Services;
    using AlwaysDecrypted.Setup;
    using Autofac;
    using System;
    using System.Threading.Tasks;

    public class Program
    {
        static async Task Main(string[] args)
        {
			// TODO: Use arguments...
			try
			{
				Logger.Log($"Execution started at {DateTime.Now}");
				Setup();
				await Run();
				Logger.Log($"Execution finished at {DateTime.Now}");
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}
		}

		private static void Setup()
		{
			Logger.Log("Setting up dependencies");

			// Build dependency container
			DependencyBuilder.Build().BeginLifetimeScope();
		}

		private static async Task Run()
		{
			// Get decryption service and decrypt everything
			var decryptionService = DependencyBuilder.Container.Resolve<IDataDecryptionService>();
			await decryptionService.DecryptColumns();

			Logger.Log($"The database was successfully decrypted");
		}
	}
}
