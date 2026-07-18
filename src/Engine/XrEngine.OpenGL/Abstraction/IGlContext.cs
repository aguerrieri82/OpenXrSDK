#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif


namespace XrEngine.OpenGL
{
    public interface IGlContext : IDisposable
    {
        GL Gl { get; }

        void Take();

        void Release();

        Thread? OwnerThread { get; }
    }
}
