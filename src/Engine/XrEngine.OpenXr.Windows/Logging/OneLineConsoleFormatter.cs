using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

using LogLevel2 = Microsoft.Extensions.Logging.LogLevel;

namespace XrEngine.OpenXr.Windows
{
    public class OneLineConsoleFormatter : ConsoleFormatter
    {
        private readonly IDisposable? _optionsReloadToken;
        private ConsoleFormatterOptions _formatterOptions;

        public OneLineConsoleFormatter(IOptionsMonitor<ConsoleFormatterOptions> options)

            : base(nameof(OneLineConsoleFormatter)) => (_optionsReloadToken, _formatterOptions) = (options.OnChange(ReloadLoggerOptions), options.CurrentValue);

        private void ReloadLoggerOptions(ConsoleFormatterOptions options) =>
            _formatterOptions = options;

        public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter)
        {
            string? message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception);

            if (message is null)
                return;

            var index = logEntry.Category.LastIndexOf(".");
            var category = index == -1 ? logEntry.Category : logEntry.Category.Substring(index + 1);

            var color = GetLogLevelConsoleColors(logEntry.LogLevel);

            textWriter.WriteColoredMessage(string.Format("{0:HH:mm:ss.fff}", DateTime.Now), null, ConsoleColor.Gray);
            textWriter.WriteColoredMessage($" [{category}] ", null, ConsoleColor.Blue);
            textWriter.WriteColoredMessage($"{message}", null, color);
            textWriter.WriteLine();
            if (logEntry.Exception != null)
            {
                textWriter.WriteColoredMessage(logEntry.Exception.ToString(), null, color);
                textWriter.WriteLine();
            }
        }

        private static ConsoleColor GetLogLevelConsoleColors(LogLevel2 logLevel)
        {

            return logLevel switch
            {
                LogLevel2.Trace => ConsoleColor.Gray,
                LogLevel2.Debug => ConsoleColor.White,
                LogLevel2.Information => ConsoleColor.Green,
                LogLevel2.Warning => ConsoleColor.Yellow,
                LogLevel2.Error => ConsoleColor.Red,
                LogLevel2.Critical => ConsoleColor.Red,
                _ => ConsoleColor.Gray,
            };
        }

        public void Dispose() => _optionsReloadToken?.Dispose();
    }
}
