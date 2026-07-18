

namespace XrEngine.OpenXr
{
    public interface IRenderSurfaceProvider
    {
        IRenderSurface CreateRenderSurface(GraphicDriver driver);

        IRenderSurface RenderSurface { get; }
    }
}
