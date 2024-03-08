using System.Diagnostics;

namespace XrEngine.OpenGL
{
    public static class GlDebug
    {
        [Conditional("LOGGL")]
        public static void Log(string message)
        {
            Console.WriteLine(message);
        }
    }
}
