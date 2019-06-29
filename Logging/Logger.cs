namespace AlwaysDecrypted.Logging
{
	using System;

	public class Logger : ILogger
	{
		public void Log(string message)
		{
			Console.WriteLine(message);
		}

		public void Log(string message, LogEventLevel level)
		{
			if(level == LogEventLevel.Error)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine(message);
				Console.ResetColor();
				return;
			}

			if (level == LogEventLevel.Warning)
			{
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine(message);
				Console.ResetColor();
				return;
			}

			Console.WriteLine(message);
		}
	}
}
