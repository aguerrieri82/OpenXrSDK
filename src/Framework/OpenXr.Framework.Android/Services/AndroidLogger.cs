using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using LogA = Android.Util.Log;

namespace OpenXr.Framework.Android
{
    public class AndroidLoggerFactory : ILoggerProvider
    {
        readonly ConcurrentDictionary<string, ILogger> _loggers = [];

        public ILogger CreateLogger(string categoryName)
        {

            return _loggers.GetOrAdd(categoryName, cat => new AndroidLogger(categoryName));
        }

        public void Dispose()
        {
            _loggers.Clear();
        }
    }

    public class AndroidLogger : ILogger
    {
        readonly string _categoryName;

        public AndroidLogger(string categoryName)
        {
            _categoryName = categoryName;
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
            var msg = formatter(state, exception);
            switch (logLevel)
            {
                case LogLevel.Debug:
                    LogA.Debug(_categoryName, msg);
                    break;
                case LogLevel.Information:
                    LogA.Info(_categoryName, msg);
                    break;
                case LogLevel.Error:
                    LogA.Error(_categoryName, msg);
                    break;
                case LogLevel.Warning:
                    LogA.Warn(_categoryName, msg);
                    break;
            }
        }
    }
}
