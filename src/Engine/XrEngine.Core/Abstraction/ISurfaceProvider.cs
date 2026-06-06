using SkiaSharp;

namespace XrEngine
{
    public interface ISurfaceProvider
    {
        SKSurface CreateSurface(Texture2D texture, nint handle = 0);

        void BeginDrawSurface(SKSurface surface, Texture2D texture);

        void EndDrawSurface(SKSurface surface, Texture2D texture);
    }
}
