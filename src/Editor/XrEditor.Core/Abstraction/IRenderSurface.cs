using System.Numerics;
using XrEngine;
using XrInteraction;


namespace XrEditor
{
    public interface IRenderSurface : IPointer2EventSource
    {
        event EventHandler SizeChanged;

        event EventHandler Ready;

        IRenderEngine CreateRenderEngine(object? driverOptions);

        void ReleaseContext();

        bool TakeContext();

        void SwapBuffers();

        void EnableVSync(bool enable);

        public bool SupportsDualRender { get; }

        Vector2 Size { get; }

        IntPtr HWnd { get; }

    }
}
