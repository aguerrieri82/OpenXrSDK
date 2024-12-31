#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

namespace XrEngine.OpenGL
{
    public interface IGlRenderAttachment : IGlObject, IDisposable
    {
        uint Width { get; }

        uint Height { get; }

        InternalFormat InternalFormat { get; }
    }
}
