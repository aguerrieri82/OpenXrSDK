namespace XrEngine
{
    public static class Log
    {
        public static void Info(object source, string message, params object?[] args)
        {
            Logger.LogMessage(string.Format(message, args), LogLevel.Info);
        }

        public static void Warn(object source, string message, params object?[] args)
        {
            Logger.LogMessage(string.Format(message, args), LogLevel.Warning);
        }

        public static void Debug(object source, string message, params object?[] args)
        {
            Logger.LogMessage(string.Format(message, args), LogLevel.Debug);
        }

        public static void Error(object source, Exception exception)
        {
            Logger.LogMessage(exception.ToString(), LogLevel.Error);
        }


        public static IProgressLogger Logger => Context.Require<IProgressLogger>();
    }
}
