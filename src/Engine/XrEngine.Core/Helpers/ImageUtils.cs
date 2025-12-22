using Common.Interop;
using SkiaSharp;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace XrEngine
{
    public static class ImageUtils
    {
        static readonly Dictionary<TextureFormat, SKColorType> FORMAT_TO_SKIA = new()
        {
            { TextureFormat.Bgra32,      SKColorType.Bgra8888 },
            { TextureFormat.SBgra32,     SKColorType.Bgra8888 },
            { TextureFormat.Rgba32,      SKColorType.Rgba8888 },
            { TextureFormat.SRgba32,     SKColorType.Srgba8888 },
            { TextureFormat.GrayInt8,    SKColorType.Gray8 },
            { TextureFormat.RgbaFloat16, SKColorType.RgbaF16 },
            { TextureFormat.RgbaFloat32, SKColorType.RgbaF32 },
        };


        static readonly Dictionary<SKColorType, TextureFormat[]> SKIA_TO_FORMATS = new()
        {
            { SKColorType.Bgra8888,  new[] { TextureFormat.Bgra32, TextureFormat.SBgra32 } },
            { SKColorType.Rgba8888,  new[] { TextureFormat.Rgba32 } },
            { SKColorType.Srgba8888, new[] { TextureFormat.SRgba32 } },
            { SKColorType.Gray8,     new[] { TextureFormat.GrayInt8 } },
            { SKColorType.RgbaF16,   new[] { TextureFormat.RgbaFloat16 } },
            { SKColorType.RgbaF32,   new[] { TextureFormat.RgbaFloat32 } },
        };

        public static bool IsBgr(this TextureFormat format)
        {
            return format == TextureFormat.Bgra32 || format == TextureFormat.SBgra32;
        }

        public static uint GetPixelSizeBit(this TextureFormat format)
        {
            return format switch
            {
                TextureFormat.Rg88 => 16,
                TextureFormat.Rgba32 => 32,
                TextureFormat.SRgba32 => 32,
                TextureFormat.Bgra32 => 32,
                TextureFormat.Rgb24 => 24,
                TextureFormat.SRgb24 => 24,
                TextureFormat.RgbFloat32 => 32 * 3,
                TextureFormat.RgbaFloat32 => 32 * 4,
                TextureFormat.RgbaFloat16 => 16 * 4,
                TextureFormat.Depth24Float => 24,
                TextureFormat.Depth16 => 16,
                TextureFormat.GrayInt8 => 8,
                TextureFormat.GrayInt16 => 16,
                TextureFormat.GrayRawSInt16 => 16,
                TextureFormat.RgFloat16 => 16 * 2,
                _ => throw new NotSupportedException()
            };

        }


        public static bool IsFloat(this TextureFormat format)
        {
            return format.IsFloat16() || format.IsFloat32();
        }

        public static bool IsSrgb(this TextureFormat format)
        {
            return format == TextureFormat.SRgb24 ||
                   format == TextureFormat.SRgba32 ||
                   format == TextureFormat.SBgra32;
        }

        public static bool IsInt8(this TextureFormat format)
        {
            return format == TextureFormat.Rg88 ||
                   format == TextureFormat.Rgb24 ||
                   format == TextureFormat.Rgba32 ||
                   format == TextureFormat.GrayInt8 ||
                   format == TextureFormat.Bgra32 ||
                   format == TextureFormat.SRgba32 ||
                   format == TextureFormat.SBgra32 ||
                    format == TextureFormat.SRgb24;
        }

        public static bool IsFloat16(this TextureFormat format)
        {
            return format == TextureFormat.RgFloat16 ||
                   format == TextureFormat.RgbFloat16 ||
                   format == TextureFormat.RgbaFloat16;
        }

        public static bool IsFloat32(this TextureFormat format)
        {
            return format == TextureFormat.RgFloat32 ||
                   format == TextureFormat.RgbFloat32 ||
                   format == TextureFormat.RgbaFloat32 ||
                   format == TextureFormat.GrayFloat32;
        }

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
            EngineNativeLib.ImageCopyChannel(pMetal, pDst, metal.Width, metal.Height, metal.Width * GetPixelSizeByte(metal.Format), metal.Width * 4, 0, 2, 1);
            EngineNativeLib.ImageCopyChannel(pRough, pDst, roughness.Width, roughness.Height, roughness.Width * GetPixelSizeByte(roughness.Format), metal.Width * 4, 0, 1, 1);

            var tex = new Texture2D
            {
                MipLevelCount = 20,
                MinFilter = ScaleFilter.LinearMipmapLinear
            };

            tex.LoadData(new TextureData
            {
                Data = mrImage,
                Width = metal.Width,
                Height = metal.Height,
                Format = TextureFormat.Rgba32
            });
            return tex;
        }

        public static Texture2D MergeMetalRaugh(Texture2D roughness)
        {
            var metalData = MemoryBuffer.Create<byte>(roughness.Width * roughness.Height * 1);
            metalData.AsSpan().Fill(255);

            var texData = new TextureData
            {
                Data = metalData,
                Width = roughness.Width,
                Height = roughness.Height,
                Format = TextureFormat.GrayInt8
            };


            return MergeMetalRaugh(texData, roughness.Data![0]);
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
            if (!FORMAT_TO_SKIA.TryGetValue(format, out var color))
                throw new NotSupportedException();
            return color;
        }

        public static TextureFormat GetFormat(SKColorType color)
        {
            if (!SKIA_TO_FORMATS.TryGetValue(color, out var format))
                throw new NotSupportedException();
            return format[0];
        }

        public static SKBitmap? ToBitmap(this TextureData data, bool flipY)
        {
            if (data.Height == 0 || data.Width == 0)
                return null;

            var pixelSize = GetPixelSizeByte(data.Format);

            var image = new SKBitmap((int)data.Width, (int)data.Height, GetSkFormat(data.Format), SKAlphaType.Opaque);

            Debug.Assert(data.Data != null);

            Debug.Assert(image.RowBytes == data.Width * pixelSize);

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

        public static unsafe TextureData ToTextureData(this SKBitmap image)
        {
            var buffer = MemoryBuffer.Create<byte>((uint)(image.BytesPerPixel * image.Width * image.Height));

            fixed (byte* pSrc = image.GetPixelSpan())
            {
                using var dst = buffer.MemoryLock();
                EngineNativeLib.CopyMemory((nint)pSrc, dst, buffer.Size);
            }

            return new TextureData
            {
                Width = (uint)image.Width,
                Height = (uint)image.Height,
                Format = GetFormat(image.ColorType),
                Data = buffer,
            };
        }

        public static TextureData Resize(TextureData data, int width, int height, ref SKBitmap? image)
        {
            if (width == data.Width && height == data.Height)
                return data;

            image ??= ToBitmap(data, false)!;

            var so = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);

            using var newImage = image.Resize(new SKSizeI(width, height), so);

            return newImage.ToTextureData();
        }

        public static unsafe TextureData Pack(TextureData data, int align)
        {
            var pWidth = (int)MathF.Ceiling(data.Width / (float)align) * align;
            var pHeight = (int)MathF.Ceiling(data.Height / (float)align) * align;

            if (pWidth == data.Width && pHeight == data.Height)
                return data;

            var result = data.Clone();

            var pixelSize = data.Format.GetPixelSizeBit() / 8;

            var newData = MemoryBuffer.Create<byte>((uint)(pWidth * pixelSize * pHeight));

            using var pSrc = result.Data!.MemoryLock();

            using var pDst = newData.MemoryLock();

            EngineNativeLib.ImagePack(data.Width, data.Height, pSrc, (uint)pWidth, (uint)pHeight, pDst, pixelSize);

            result.Data = newData;
            result.Width = (uint)pWidth;
            result.Height = (uint)pHeight;

            return result;

        }

        public static unsafe IMemoryBuffer<byte> ConvertShortToFloat(IMemoryBuffer<byte> data)
        {
            var i = 0;
            var length = (int)data.Size / 2;
            var vectorSize = Vector128<short>.Count;

            var result = MemoryBuffer.Create<byte>((uint)length * sizeof(float));

            using var src = data.MemoryLock();
            using var dst = result.MemoryLock();

            var dstFloat = (float*)dst.Data;
            var srcShort = (short*)src.Data;

            if (Avx2.IsSupported)
            {
                for (; i <= length - vectorSize; i += vectorSize)
                {
                    var shortVector = Unsafe.Read<Vector128<short>>(srcShort + i);

                    var intVector = Avx2.ConvertToVector256Int32(shortVector);

                    var floatVector = Avx2.ConvertToVector256Single(intVector);

                    floatVector.Store(dstFloat + i);
                }
            }

            for (; i < length; i++)
                dstFloat[i] = srcShort[i];

            return result;
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
