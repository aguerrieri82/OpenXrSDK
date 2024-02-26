using System.Numerics;
using Xr.Engine;



namespace Xr.Editor
{
    public interface IRenderSurface : IPointerEventSource
    {
        event EventHandler SizeChanged;

        event EventHandler Ready;

        IRenderEngine CreateRenderEngine();

        void ReleaseContext();

        void TakeContext();

        void SwapBuffers();

        void EnableVSync(bool enable);

        Vector2 Size { get; }

        IntPtr HWnd { get; }

    }
}
