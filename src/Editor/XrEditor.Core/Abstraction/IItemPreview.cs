using SkiaSharp;

namespace XrEditor
{
    public interface IItemPreview
    {
        Task<SKBitmap> CreatePreviewAsync();
    }
}
