#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

namespace XrEngine.OpenGL
{
    public interface IGlObject
    {
        public uint Handle { get; }

        public GL GL { get; }

    }
}
