using System.Diagnostics;

namespace OpenXr.Engine.OpenGL
{
    public static class GlDebug
    {
        [Conditional("DEBUG")]
        public static void Log(string message)
        {
            Console.WriteLine(message);
        }
    }
}
