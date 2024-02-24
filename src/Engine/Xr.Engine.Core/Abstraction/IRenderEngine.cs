namespace Xr.Engine
{
    public interface IRenderEngine : IDisposable
    {
        void Render(Scene scene, Camera camera, Rect2I view);

        Texture2D? GetDepth();

        Rect2I View { get; }
    }
}
