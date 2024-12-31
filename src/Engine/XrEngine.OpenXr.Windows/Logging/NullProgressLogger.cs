namespace XrEngine.OpenXr.Windows
{
    public class NullProgressLogger : IProgressLogger
    {
        public void BeginTask(object source, string? message = null)
        {

        }

        public void EndTask()
        {

        }

        public void LogMessage(object source, string text, LogLevel level = LogLevel.Info, bool retain = false)
        {

        }

        public void LogProgress(object source, double current, double total, string? message = null, bool retain = false)
        {

        }
    }
}

