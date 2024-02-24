using System.Diagnostics;

namespace Xr.Engine.OpenGL
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
