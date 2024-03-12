using System.Numerics;
using XrEngine;



namespace XrEditor
{
    public interface IRenderSurface : IPointerEventSource
    {
        event EventHandler SizeChanged;

        event EventHandler Ready;

        IRenderEngine CreateRenderEngine();

        void ReleaseContext();

        bool TakeContext();

        void SwapBuffers();

        void EnableVSync(bool enable);

        public bool SupportsDualRender { get; }

        Vector2 Size { get; }

        IntPtr HWnd { get; }

    }
}
