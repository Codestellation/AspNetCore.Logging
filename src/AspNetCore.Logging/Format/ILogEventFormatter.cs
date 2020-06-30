namespace Codestellation.AspNetCore.Logging.Format
{
    public interface ILogEventFormatter
    {
        string Format(HttpContextLogEvent logEvent);
    }
}