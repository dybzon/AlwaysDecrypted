namespace AlwaysDecrypted.Settings
{
    using AlwaysDecrypted.Logging;
    using System.Collections.Generic;
    using System.Linq;

    public class SettingsBuilder : ISettingsBuilder
	{
		private readonly ICollection<string> validArguments = new List<string> { "-db", "-database", "-server", "-tables" };

		public SettingsBuilder(ILogger logger, ISettings settings)
		{
			Logger = logger;
			Settings = settings;
		}

		private ILogger Logger { get; }
		private ISettings Settings { get; }

		public void BuildSettings(string[] args)
		{
			var parsedArgs = this.ParseArguments(args);

			this.ValidateArguments(parsedArgs);
			foreach(var arg in parsedArgs.Where(a => validArguments.Contains(a.Key)))
			{
				if(arg.Key.Equals("-db") || arg.Key.Equals("-database"))
				{
					this.Settings.Database = arg.Value;
					continue;
				}

				if (arg.Key.Equals("-server"))
				{
					this.Settings.Server = arg.Value;
					continue;
				}

				if (arg.Key.Equals("-tables"))
				{
					this.Settings.TablesToDecrypt = arg.Value.Split(',').Select(t => t.Trim());
					continue;
				}
			}
		}

		private IDictionary<string, string> ParseArguments(string[] args) 
			=> args.Select(s => s.Split(new[] { '=' }, 2)).ToDictionary(s => s[0].ToLower(), s => s[1]);

		private void ValidateArguments(IDictionary<string, string> args)
		{
			var invalidArguments = args.Where(a => !validArguments.Contains(a.Key));
			if (invalidArguments.Any())
			{
				this.Logger.Log($"Invalid arguments were given: {string.Join(", ", invalidArguments.Select(a => a.Key))}", LogEventLevel.Warning);
			}
		}
	}
}
