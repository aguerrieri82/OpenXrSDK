#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif


namespace OpenXr.Framework.OpenGL
{
    public interface IOpenGLDevice
    {
        nint HDc { get; }

        nint GlCtx { get; }

        GL Gl { get; }
    }
}
