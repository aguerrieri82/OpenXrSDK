using System.Diagnostics;

namespace XrEngine.OpenGL
{
    public static class GlDebug
    {
        [Conditional("LOGGL")]
        public static void Log(object sender, string message, params object?[] args)
        {
            Console.WriteLine(message, args);
        }
    }
}
