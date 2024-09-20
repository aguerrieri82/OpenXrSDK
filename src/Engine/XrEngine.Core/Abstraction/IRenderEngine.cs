using XrMath;

namespace XrEngine
{
    public interface IRenderEngine : IDisposable
    {
        void Render(Scene3D scene, Camera camera, Rect2I view, bool flush);

        void SetDefaultRenderTarget();

        void SetRenderTarget(Texture2D texture);

        void Suspend();

        void Resume();

        Texture2D? GetDepth();

        Texture2D? GetShadowMap();  

        IDispatcher Dispatcher { get; }
    }
}
