using System.Numerics;
using XrInteraction;


namespace XrEngine
{
    public interface IRenderSurface : IPointer2EventSource
    {
        event EventHandler SizeChanged;

        event EventHandler Ready;

        IRenderEngine CreateRenderEngine(object? driverOptions);

        void ReleaseContext();

        bool TakeContext();

        void SwapBuffers();

        void EnableVSync(bool enable, int scale = 1);

        void BeginFrame(long frameNum);

        void EndFrame();

        public bool SupportsDualRender { get; }

        Vector2 Size { get; }

        IntPtr HWnd { get; }

    }
}
