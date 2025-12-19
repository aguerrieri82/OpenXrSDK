using XrMath;

namespace XrEngine
{
    public static class Log
    {

        public static void Info(object source, string message, params object?[] args)
        {
            Logger.LogMessage(source, string.Format(message, args), LogLevel.Info);
        }

        public static void Warn(object source, string message, params object?[] args)
        {
            Logger.LogMessage(source, string.Format(message, args), LogLevel.Warning);
        }

        public static void Debug(object source, string message, params object?[] args)
        {
            Logger.LogMessage(source, string.Format(message, args), LogLevel.Debug);
        }

        public static void Error(object source, Exception exception, string message = "{0}")
        {
            Logger.LogMessage(source, string.Format(message, exception), LogLevel.Error);
        }

        public static void Value<T>(string name, T value)
        {

            TimeLogger.LogValue(name, value);
        }

        public static void Checkpoint(string name, Color color)
        {
            TimeLogger.Checkpoint(name, color);
        }


        public static IProgressLogger Logger => Context.Require<IProgressLogger>();

        public static ITimeLogger TimeLogger => Context.Require<ITimeLogger>();
    }
}
