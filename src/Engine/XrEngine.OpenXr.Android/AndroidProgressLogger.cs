using LogA = Android.Util.Log;

namespace XrEngine.OpenXr
{
    internal class AndroidProgressLogger : IProgressLogger
    {
        string _tag;

        public AndroidProgressLogger(string tag = "XrApp")
        {
            _tag = tag; 
        }

        public void BeginTask(string? message = null)
        {

        }

        public void EndTask()
        {

        }

        public void LogMessage(string text, LogLevel level = LogLevel.Info, bool retain = false)
        {
            switch (level)
            {
                case LogLevel.Debug:
                    LogA.Debug(_tag, text);
                    break;
                case LogLevel.Info:
                    LogA.Info(_tag, text);
                    break;
                case LogLevel.Error:
                    LogA.Error(_tag, text);
                    break;
                case LogLevel.Warning:
                    LogA.Warn(_tag, text);
                    break;
            }
        }

        public void LogProgress(double current, double total, string? message = null, bool retain = false)
        {

        }
    }
}
