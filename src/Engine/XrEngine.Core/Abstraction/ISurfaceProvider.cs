using SkiaSharp;

namespace XrEngine
{
    public interface ISurfaceProvider
    {
        SKSurface CreateSurface(Texture2D texture, nint handle = 0);
    }
}
