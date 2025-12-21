using Microsoft.Extensions.Logging;

namespace CanvasUI
{
    public class TextBlockLogger : ILogger
    {
        readonly TextBlock _textBlock;
        readonly int _maxLines;
        readonly List<string> _lines = [];

        public TextBlockLogger(TextBlock block, int maxLines)
        {
            _textBlock = block;
            _maxLines = maxLines;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Log(formatter(state, exception));
        }

        public void Log(string text)
        {
            lock (_lines)
            {
                _lines.Insert(0, text);

                while (_lines.Count > _maxLines)
                    _lines.RemoveAt(_lines.Count - 1);

                _textBlock.Text = string.Join('\n', _lines);
            }
        }
    }
}
