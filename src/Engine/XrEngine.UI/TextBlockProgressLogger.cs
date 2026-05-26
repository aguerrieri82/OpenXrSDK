using CanvasUI;

namespace XrEngine.UI
{
    public class TextBlockProgressLogger : TextBlockLogger, IProgressLogger
    {
        public TextBlockProgressLogger(TextBlock block, int maxLines)
            : base(block, maxLines)
        {

        }

        public void BeginTask(object source, string? message = null)
        {

        }

        public void EndTask()
        {

        }

        public void LogMessage(object source, string text, LogLevel level = LogLevel.Info, bool retain = false)
        {
            Log(text);
        }

        public void LogProgress(object source, double current, double total, string? message = null, bool retain = false)
        {

        }
    }
}
