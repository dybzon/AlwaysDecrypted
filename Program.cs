namespace AlwaysDecrypted
{
    using AlwaysDecrypted.Logging;
    using AlwaysDecrypted.Services;
    using AlwaysDecrypted.Settings;
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
				Setup(args);
				await Run();
			}
			catch (Exception e)
			{
				Logger.Log(e.Message, LogEventLevel.Error);
			}
		}

		private static ILogger Logger { get; set; }

		private static void Setup(string[] args)
		{
			// Build dependency container
			DependencyBuilder.Build().BeginLifetimeScope();
			DependencyBuilder.Container.Resolve<ISettingsBuilder>().BuildSettings(args);
			Logger = DependencyBuilder.Container.Resolve<ILogger>();
			Logger.Log("Setting up dependencies", LogEventLevel.Information);
		}

		private static async Task Run()
		{
			Logger.Log($"Execution started at {DateTime.Now}", LogEventLevel.Information);
			// Get decryption service and decrypt everything
			var decryptionService = DependencyBuilder.Container.Resolve<IDataDecryptionService>();
			await decryptionService.DecryptColumns();

			Logger.Log($"The database was successfully decrypted", LogEventLevel.Information);
			Logger.Log($"Execution finished at {DateTime.Now}", LogEventLevel.Information);
		}
	}
}
