using Microsoft.Extensions.Logging;
using LogLevelMs = Microsoft.Extensions.Logging.LogLevel;

namespace XrEngine.OpenXr
{
    public class NetLoggerProgressLogger : ILogger
    {
        public class LogScope : IDisposable
        {
            NetLoggerProgressLogger _self;

            public LogScope(NetLoggerProgressLogger self, object? state)
            {
                _self = self;
                _self._scope = state;
            }

            public void Dispose()
            {
                _self._scope = null;
            }
            public object? State;
        }

        protected IProgressLogger _logger;
        protected object? _scope;


        public NetLoggerProgressLogger()
        {
            _logger = Context.Require<IProgressLogger>();
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return new LogScope(this, state);
        }

        public bool IsEnabled(LogLevelMs logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevelMs logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var level = logLevel switch
            {
                LogLevelMs.Trace => LogLevel.Debug,
                LogLevelMs.Debug => LogLevel.Debug,
                LogLevelMs.Information => LogLevel.Info,
                LogLevelMs.Warning => LogLevel.Warning,
                LogLevelMs.Error => LogLevel.Error,
                LogLevelMs.Critical => LogLevel.Error,
                LogLevelMs.None => LogLevel.Debug,
                _ => LogLevel.Debug
            };

            _logger.LogMessage(_scope ?? "", formatter(state, exception), level);
        }
    }
}
