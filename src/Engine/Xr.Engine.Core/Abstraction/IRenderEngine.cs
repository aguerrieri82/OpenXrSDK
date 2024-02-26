namespace Xr.Engine
{
    public interface IRenderEngine : IDisposable
    {
        void Render(Scene scene, Camera camera, Rect2I view);

        void SetDefaultRenderTarget();

        void ReleaseContext(bool release);

        Texture2D? GetDepth();

        Rect2I View { get; }
    }
}
