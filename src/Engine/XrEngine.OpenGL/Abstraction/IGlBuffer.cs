#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif


namespace XrEngine.OpenGL
{
    public interface IGlBuffer : IBuffer, IDisposable
    {
        void Bind();

        void Unbind();

        BufferTargetARB Target { get; }

        uint Handle { get; }
    }
}
