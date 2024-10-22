using LogA = Android.Util.Log;

namespace XrEngine.OpenXr
{
    internal class AndroidProgressLogger : IProgressLogger
    {

        public void BeginTask(object source, string? message = null)
        {

        }

        public void EndTask()
        {

        }

        public void LogMessage(object source, string text, LogLevel level = LogLevel.Info, bool retain = false)
        {
            var tag = source.GetType().Name;

            switch (level)
            {
                case LogLevel.Debug:
                    LogA.Debug(tag, text);
                    break;
                case LogLevel.Info:
                    LogA.Info(tag, text);
                    break;
                case LogLevel.Error:
                    LogA.Error(tag, text);
                    break;
                case LogLevel.Warning:
                    LogA.Warn(tag, text);
                    break;
            }
        }

        public void LogProgress(object source, double current, double total, string? message = null, bool retain = false)
        {

        }
    }
}
