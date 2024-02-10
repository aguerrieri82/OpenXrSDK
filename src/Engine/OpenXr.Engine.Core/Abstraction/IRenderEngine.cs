namespace OpenXr.Engine
{
    public interface IRenderEngine : IDisposable
    {
        void Render(Scene scene, Camera camera, RectI view);
    }
}
