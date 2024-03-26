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
            { SKColorType.RgbaF16, TextureFormat.RgbaFloat16 },
            { SKColorType.RgbaF32, TextureFormat.RgbaFloat32 },
        };


        public static uint GetPixelSizeByte(SKColorType type)
        {
            switch (type)
            {
                case SKColorType.Gray8:
                    return 1;
                case SKColorType.Srgba8888:
                case SKColorType.Rgba8888:
                case SKColorType.Bgra8888:
                    return 4;
                case SKColorType.RgbaF16:
                    return 8;
                case SKColorType.RgbaF32:
                    return 16;
                default:
                    throw new NotSupportedException();
            }
        }

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
            //do always for  SKAlphaType.Unpremul
            /*
            if (src.ColorType == dest)
                return src;
            */

            var newInfo = new SKImageInfo(src.Info.Width, src.Info.Height, dest, SKAlphaType.Unpremul, src.Info.ColorSpace);

            var newBitmap = new SKBitmap(newInfo);

            using var canvas = new SKCanvas(newBitmap);

            canvas.Clear(new SKColor(1, 1, 1, 1));

            using var paint  = new SKPaint();
            paint.BlendMode = SKBlendMode.DstOver;

            canvas.DrawBitmap(src, 0, 0, paint);

            src.Dispose();

            return newBitmap;
        }

    }
}
