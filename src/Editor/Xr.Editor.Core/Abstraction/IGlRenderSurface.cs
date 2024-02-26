#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif


namespace Xr.Editor
{
    public interface IGlRenderSurface : IRenderSurface
    {
        IntPtr GlCtx { get; }

        IntPtr Hdc { get; }
        
        GL? Gl { get; }
    }
}
