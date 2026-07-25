using System.Diagnostics;

namespace XrEngine.OpenGL
{
    public static class GlDebug
    {
        [Conditional("DEBUG")]
        public static void Log(object sender, string message, params object?[] args)
        {
            if (sender is GlTexture)
                Logger?.Invoke(sender, message, args);
        }

        public static Action<object, string, object?[]>? Logger;


        public static bool TrackBuffers = false;
    }
}
