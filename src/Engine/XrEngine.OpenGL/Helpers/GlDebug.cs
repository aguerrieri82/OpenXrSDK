using System.Diagnostics;

namespace XrEngine.OpenGL
{
    public static class GlDebug
    {
        [Conditional("DEBUG")]
        public static void Log(object sender, string message, params object?[] args)
        {
            Logger?.Invoke(sender, message, args);
        }

        public static Action<object, string, object?[]>? Logger;
    }
}
