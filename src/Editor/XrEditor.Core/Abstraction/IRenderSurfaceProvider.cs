using XrEngine.OpenXr;

namespace XrEditor
{
    public interface IRenderSurfaceProvider
    {
        IRenderSurface CreateRenderSurface(GraphicDriver driver);

        IRenderSurface RenderSurface { get; }
    }
}
