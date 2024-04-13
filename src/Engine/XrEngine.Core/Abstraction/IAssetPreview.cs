using SkiaSharp;
using XrMath;

namespace XrEngine
{

    public interface IAssetPreview
    {
        SKBitmap? CreatePreview(Size2 size);
    }
}
