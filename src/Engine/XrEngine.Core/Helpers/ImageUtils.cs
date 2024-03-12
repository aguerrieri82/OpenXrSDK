using SkiaSharp;

namespace XrEngine
{
    public static class ImageUtils
    {
        static readonly Dictionary<SKColorType, TextureFormat> FORMAT_MAP = new() {
            { SKColorType.Bgra8888, TextureFormat.Bgra32 },
            { SKColorType.Rgba8888, TextureFormat.Rgba32 },
            { SKColorType.Srgba8888, TextureFormat.SRgba32 },
            { SKColorType.Gray8, TextureFormat.Gray8 },
        };

        public static SKColorType GetFormat(TextureFormat format)
        {
            return FORMAT_MAP.First(a => a.Value == format).Key;
        }

        public static TextureFormat GetFormat(SKColorType color)
        {
            if (!FORMAT_MAP.TryGetValue(color, out TextureFormat format))
                throw new NotSupportedException();
            return format;
        }

        public static SKBitmap ChangeColorSpace(SKBitmap src, SKColorType dest)
        {
            if (src.ColorType == dest)
                return src;

            var newInfo = new SKImageInfo(src.Width, src.Height, dest);

            var newBitmap = new SKBitmap(newInfo);

            using var canvas = new SKCanvas(newBitmap);

            canvas.Clear(SKColors.Transparent);

            canvas.DrawBitmap(src, 0, 0);

            src.Dispose();

            return newBitmap;
        }

    }
}
