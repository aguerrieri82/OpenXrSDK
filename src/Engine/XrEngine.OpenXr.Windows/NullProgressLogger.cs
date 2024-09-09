using XrEngine;

namespace XrEngine.OpenXr.Windows
{
    public class NullProgressLogger : IProgressLogger
    {
        public void BeginTask(string? message = null)
        {

        }

        public void EndTask()
        {

        }

        public void LogMessage(string text, LogLevel level = LogLevel.Info, bool retain = false)
        {

        }

        public void LogProgress(double current, double total, string? message = null, bool retain = false)
        {

        }
    }
}

