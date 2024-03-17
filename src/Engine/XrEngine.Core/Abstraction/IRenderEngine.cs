using XrMath;

namespace XrEngine
{
    public interface IRenderEngine : IDisposable
    {
        void Render(Scene3D scene, Camera camera, Rect2I view, bool flush);

        void SetDefaultRenderTarget();

        void Suspend();

        void Resume();

        Texture2D? GetDepth();
    }
}
