#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Numerics;



namespace Xr.Editor
{
    public interface IRenderSurface : IPointerEventSource
    {
        event EventHandler SizeChanged;

        event EventHandler Ready;

        void ReleaseContext();

        void TakeContext();

        void SwapBuffers();

        void EnableVSync(bool enable);

        Vector2 Size { get; }

        IntPtr Hdc { get; }

        IntPtr GlCtx { get; }

        IntPtr HWnd { get; }

        GL? Gl { get; }
    }
}
