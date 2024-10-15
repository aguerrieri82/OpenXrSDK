namespace XrEngine
{
    public enum LogLevel
    {
        Info,
        Error,
        Success,
        Warning,
        Debug
    }

    public interface IProgressLogger
    {
        void BeginTask(object source, string? message = null);

        void EndTask();

        void LogMessage(object source, string text, LogLevel level = LogLevel.Info, bool retain = false);

        void LogProgress(object source, double current, double total, string? message = null, bool retain = false);
    }
}
