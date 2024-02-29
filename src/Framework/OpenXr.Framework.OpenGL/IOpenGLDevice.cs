
using Silk.NET.OpenGL;

namespace OpenXr.Framework.OpenGL
{
    public interface IOpenGLDevice
    {
        nint HDc { get; }

        nint GlCtx { get; }

        GL Gl { get; }
    }
}
