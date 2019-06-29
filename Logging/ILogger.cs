namespace AlwaysDecrypted.Logging
{
	public interface ILogger
	{
		void Log(string message);

		void Log(string message, LogEventLevel level);
	}
}
