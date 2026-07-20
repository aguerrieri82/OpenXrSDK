namespace XrEngine.OpenXr.Windows
{
    internal static class TextWriterExtensions
    {
        public static void WriteColoredMessage(this TextWriter textWriter, string message, ConsoleColor? background, ConsoleColor? foreground)
        {
            if (background != null)
                textWriter.Write(AnsiParser.GetBackgroundColorEscapeCode(background.Value));

            if (foreground != null)
                textWriter.Write(AnsiParser.GetForegroundColorEscapeCode(foreground.Value));

            textWriter.Write(message);
            
            if (foreground != null)
                textWriter.Write(AnsiParser.DefaultForegroundColor);

            if (background != null)
                textWriter.Write(AnsiParser.DefaultBackgroundColor);
        }
    }
}
