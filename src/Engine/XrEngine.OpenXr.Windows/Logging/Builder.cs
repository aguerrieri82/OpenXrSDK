using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using XrEngine.OpenXr.Windows;


namespace Microsoft.Extensions.DependencyInjection
{
    public static class Builder
    {
        public static ILoggingBuilder AddOneLineConsole(this ILoggingBuilder builder)
        {
            builder.AddConsole(options => options.FormatterName = nameof(OneLineConsoleFormatter));
            builder.AddConsoleFormatter<OneLineConsoleFormatter, ConsoleFormatterOptions>();
            return builder;
        }
    }
}
