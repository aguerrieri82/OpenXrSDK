using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanvasUI
{
    public class TextBlockLogger : ILogger
    {
        TextBlock _textBlock;
        int _maxLines;
        List<string> _lines = [];  

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
            _lines.Insert(0, formatter(state, exception));

            if (_lines.Count > _maxLines)
                _lines.RemoveAt(_lines.Count - 1);

            _textBlock.Text = string.Join('\n', _lines);
        }
    }
}
