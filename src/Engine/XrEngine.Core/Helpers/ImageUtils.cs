using Common.Interop;
using SkiaSharp;
using System.Diagnostics;

namespace XrEngine
{
    public static class ImageUtils
    {
        static readonly Dictionary<SKColorType, TextureFormat> SKIA_FORMAT_MAP = new() {
            { SKColorType.Bgra8888, TextureFormat.Bgra32 },
            { SKColorType.Rgba8888, TextureFormat.Rgba32 },
            { SKColorType.Srgba8888, TextureFormat.SRgba32 },
            { SKColorType.Gray8, TextureFormat.Gray8 },
            { SKColorType.RgbaF16, TextureFormat.RgbaFloat16 },
            { SKColorType.RgbaF32, TextureFormat.RgbaFloat32 },

        };

        public static SKBitmap ApplyGaussianBlur(SKBitmap bitmap, float radius)
        {
            using var surface = SKSurface.Create(new SKImageInfo(bitmap.Width, bitmap.Height));

            using var paint = new SKPaint
            {
                ImageFilter = SKImageFilter.CreateBlur(radius, radius),
            };

            surface.Canvas.DrawBitmap(bitmap, 0, 0, paint);
            surface.Canvas.Flush();

            return SKBitmap.FromImage(surface.Snapshot().ToRasterImage());
        }


        public static Texture2D MergeMetalRaugh(Texture2D metal, Texture2D roughness)
        {
            return MergeMetalRaugh(metal.Data![0], roughness.Data![0]);
        }

        public static Texture2D MergeMetalRaugh(TextureData metal, TextureData roughness)
        {
            var mrImage = MemoryBuffer.Create<byte>(metal.Width * metal.Height * 4);

            using var pMetal = metal.Data!.MemoryLock();
            using var pRough = roughness.Data!.MemoryLock();
            using var pDst = mrImage.MemoryLock();
            EngineNativeLib.ImageCopyChannel(pMetal, pDst, metal.Width, metal.Height, metal.Width, metal.Width * 4, 0, 2, 1);
            EngineNativeLib.ImageCopyChannel(pRough, pDst, metal.Width, metal.Height, metal.Width, metal.Width * 4, 0, 1, 1);

            var tex = new Texture2D();
            tex.LoadData(new TextureData
            {
                Data = mrImage,
                Width = metal.Width,
                Height = metal.Height,
                Format = TextureFormat.Rgba32
            });
            return tex;
        }


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

        public static uint GetPixelSizeByte(TextureFormat format)
        {
            return GetPixelSizeByte(GetSkFormat(format));
        }

        public static SKColorType GetSkFormat(TextureFormat format)
        {
            return SKIA_FORMAT_MAP.First(a => a.Value == format).Key;
        }

        public static TextureFormat GetFormat(SKColorType color)
        {
            if (!SKIA_FORMAT_MAP.TryGetValue(color, out TextureFormat format))
                throw new NotSupportedException();
            return format;
        }

        public static unsafe SKBitmap ToBitmap(TextureData data, bool flipY)
        {

            var pixelSize = GetPixelSizeByte(data.Format);

            var image = new SKBitmap((int)data.Width, (int)data.Height, GetSkFormat(data.Format), SKAlphaType.Opaque);

            Debug.Assert(data.Data != null);

            if (flipY)
            {
                var dst = MemoryBuffer.Create<byte>(data.Height * data.Width * pixelSize);

                using var pData = data.Data.MemoryLock();
                using var pDst = dst.MemoryLock();
                EngineNativeLib.ImageFlipY(pData, pDst, data.Width, data.Height, data.Width * pixelSize);
                image.SetPixels(pDst);
            }
            else
            {
                using var pData = data.Data.MemoryLock();
                image.SetPixels(pData);
            }

            return image;
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

            using var paint = new SKPaint();
            paint.BlendMode = SKBlendMode.DstOver;

            canvas.DrawBitmap(src, 0, 0, paint);

            src.Dispose();

            return newBitmap;
        }

    }
}
