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
            { TextureFormat.Bgra8,      SKColorType.Bgra8888 },
            { TextureFormat.SBgra8,     SKColorType.Bgra8888 },
            { TextureFormat.Rgba8,      SKColorType.Rgba8888 },
            { TextureFormat.SRgba8,     SKColorType.Srgba8888 },
            { TextureFormat.Gray8,    SKColorType.Gray8 },
            { TextureFormat.RgbaFloat16, SKColorType.RgbaF16 },
            { TextureFormat.RgbaFloat32, SKColorType.RgbaF32 },
        };

        static readonly Dictionary<SKColorType, TextureFormat[]> SKIA_TO_FORMATS = new()
        {
            { SKColorType.Bgra8888,  new[] { TextureFormat.Bgra8, TextureFormat.SBgra8 } },
            { SKColorType.Rgba8888,  new[] { TextureFormat.Rgba8 } },
            { SKColorType.Srgba8888, new[] { TextureFormat.SRgba8 } },
            { SKColorType.Gray8,     new[] { TextureFormat.Gray8 } },
            { SKColorType.RgbaF16,   new[] { TextureFormat.RgbaFloat16 } },
            { SKColorType.RgbaF32,   new[] { TextureFormat.RgbaFloat32 } },
        };

        public static bool IsBgr(this TextureFormat format)
        {
            return format == TextureFormat.Bgra8 ||
                   format == TextureFormat.SBgra8;
        }

        public static uint GetChannels(this TextureFormat format)
        {
            return format switch
            {
                TextureFormat.RgFloat16 or
                TextureFormat.Rg8 => 2,

                TextureFormat.SRgbaInt16 or
                TextureFormat.Rgba16 or
                TextureFormat.Rgba8 or
                TextureFormat.SRgba8 or
                TextureFormat.Bgra8 or
                TextureFormat.SBgra8 or
                TextureFormat.RgbaFloat32 or
                TextureFormat.RgbaFloat16 or
                TextureFormat.SBgra8 => 4,

                TextureFormat.Rgb8 or
                TextureFormat.RgbFloat32 or
                TextureFormat.SRgb8 => 3,

                TextureFormat.GrayFloat16 or
                TextureFormat.GrayFloat32 or
                TextureFormat.Gray8 or
                TextureFormat.Gray16 or
                TextureFormat.Gray16 => 1,

                _ => throw new NotSupportedException()
            };
        }

        public static uint GetPixelSizeBit(this TextureFormat format)
        {
            return format switch
            {
                TextureFormat.Rg8 => 16,
                TextureFormat.Rgba8 => 32,
                TextureFormat.SRgba8 => 32,
                TextureFormat.Bgra8 => 32,
                TextureFormat.Rgb8 => 24,
                TextureFormat.SRgb8 => 24,
                TextureFormat.SBgra8 => 32,
                TextureFormat.RgbFloat32 => 32 * 3,
                TextureFormat.RgbaFloat32 => 32 * 4,
                TextureFormat.RgbaFloat16 => 16 * 4,
                TextureFormat.Depth24 => 24,
                TextureFormat.Depth16 => 16,
                TextureFormat.Depth32Float => 32,
                TextureFormat.Gray8 => 8,
                TextureFormat.Gray16 => 16,
                TextureFormat.GrayInt16 => 16,
                TextureFormat.RgFloat16 => 16 * 2,
                TextureFormat.GrayFloat32 => 32,
                TextureFormat.GrayFloat16 => 16,
                TextureFormat.Rgb9e5Float => 32,
                TextureFormat.Rgba16 => 64,
                TextureFormat.SRgbaInt16 => 64,
                _ => throw new NotSupportedException()
            };
        }

        public static bool IsFloat(this TextureFormat format)
        {
            return format.IsFloat16() || format.IsFloat32() || format == TextureFormat.Rgb9e5Float;
        }

        public static bool CanGenerateMipmaps(this TextureFormat self, bool gles)
        {
            if (!gles)
                return self != TextureFormat.Unknown;

            return self is
                TextureFormat.Rgb8 or
                TextureFormat.Rgba8 or
                TextureFormat.Rg8 or
                TextureFormat.Gray8 or
                TextureFormat.RgbaFloat16 or
                TextureFormat.RgFloat16 or
                TextureFormat.GrayFloat16 or
                TextureFormat.SRgba8;
        }

        public static bool IsSrgb(this TextureFormat format)
        {
            return format == TextureFormat.SRgb8 ||
                   format == TextureFormat.SRgba8 ||
                   format == TextureFormat.SBgra8;
        }

        public static bool IsInt16(this TextureFormat format)
        {
            return format == TextureFormat.Rgba16 ||
                    format == TextureFormat.SRgbaInt16;
        }

        public static bool IsInt8(this TextureFormat format)
        {
            return format == TextureFormat.Rg8 ||
                   format == TextureFormat.Rgb8 ||
                   format == TextureFormat.Rgba8 ||
                   format == TextureFormat.Gray8 ||
                   format == TextureFormat.Bgra8 ||
                   format == TextureFormat.SRgba8 ||
                   format == TextureFormat.SBgra8 ||
                    format == TextureFormat.SRgb8;
        }

        public static bool IsFloat16(this TextureFormat format)
        {
            return format == TextureFormat.RgFloat16 ||
                   format == TextureFormat.RgbFloat16 ||
                   format == TextureFormat.RgbaFloat16 ||
                   format == TextureFormat.GrayFloat16;
        }

        public static bool IsFloat32(this TextureFormat format)
        {
            return format == TextureFormat.RgFloat32 ||
                   format == TextureFormat.RgbFloat32 ||
                   format == TextureFormat.RgbaFloat32 ||
                   format == TextureFormat.GrayFloat32;
        }

        [Obsolete]
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

            using var pMetal = metal.Content!.MemoryLock();
            using var pRough = roughness.Content!.MemoryLock();
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
                Content = mrImage,
                Width = metal.Width,
                Height = metal.Height,
                Format = TextureFormat.Rgba8
            });

            return tex;
        }

        public static Texture2D MergeMetalRaugh(Texture2D roughness)
        {
            var metalData = MemoryBuffer.Create<byte>(roughness.Width * roughness.Height * 1);
            metalData.AsSpan().Fill(255);

            var texData = new TextureData
            {
                Content = metalData,
                Width = roughness.Width,
                Height = roughness.Height,
                Format = TextureFormat.Gray8
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

        public static SKBitmap? ToBitmap(this TextureData data, bool flipY, SKAlphaType alphaType = SKAlphaType.Opaque)
        {
            if (data.Height == 0 || data.Width == 0)
                return null;

            var pixelSize = GetPixelSizeByte(data.Format);

            var image = new SKBitmap((int)data.Width, (int)data.Height, GetSkFormat(data.Format), alphaType);

            Debug.Assert(data.Content != null);

            Debug.Assert(image.RowBytes == data.Width * pixelSize);

            if (flipY)
            {
                var dst = MemoryBuffer.Create<byte>(data.Height * data.Width * pixelSize);

                using var pData = data.Content.MemoryLock();
                using var pDst = dst.MemoryLock();
                EngineNativeLib.ImageFlipY(pData, pDst, data.Width, data.Height, data.Width * pixelSize);
                image.SetPixels(pDst);
            }
            else
            {
                using var pData = data.Content.MemoryLock();
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
                Content = buffer,
            };
        }

        public static unsafe TextureData Resize(TextureData data, int width, int height)
        {
            if (width == data.Width && height == data.Height)
                return data;

            if (!data.Format.IsInt8())
                throw new NotSupportedException();

            var channels = data.Format.GetPixelSizeBit() / 8;

            var result = data.Clone();
            result.Content = MemoryBuffer.Create<byte>((uint)(channels * width * height));
            result.Width = (uint)width;
            result.Height = (uint)height;

            using var pSrc = data.Content!.MemoryLock();
            using var pDst = result.Content.MemoryLock();

            EngineNativeLib.ImageResizeBilinearU8(data.Width, data.Height, pSrc, (uint)width, (uint)height, pDst, channels);

            return result;
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

            using var pSrc = result.Content!.MemoryLock();

            using var pDst = newData.MemoryLock();

            EngineNativeLib.ImagePack(data.Width, data.Height, pSrc, (uint)pWidth, (uint)pHeight, pDst, pixelSize);

            result.Content = newData;
            result.Width = (uint)pWidth;
            result.Height = (uint)pHeight;

            return result;

        }

        public static unsafe TextureData PackToRgba8(TextureData data, int align)
        {
            if (!data.Format.IsInt8())
                throw new NotSupportedException();

            var pWidth = (int)MathF.Ceiling(data.Width / (float)align) * align;

            if (pWidth == data.Width && data.Format.GetChannels() == 4)
                return data;

            var result = data.Clone();

            var newData = MemoryBuffer.Create<byte>((uint)(pWidth * data.Height * 4));

            using var pSrc = result.Content!.MemoryLock();

            using var pDst = newData.MemoryLock();

            EngineNativeLib.ImagePackToRgba8(pSrc, pDst, data.Width, data.Height, data.Format.GetChannels(), (uint)align);

            result.Content = newData;
            result.Width = (uint)pWidth;
            result.Format = data.Format.IsBgr() ?
               (data.Format.IsSrgb() ? TextureFormat.SBgra8 : TextureFormat.Bgra8) :
               (data.Format.IsSrgb() ? TextureFormat.SRgba8 : TextureFormat.Rgba8);

            return result;

        }

        public static unsafe TextureData ConvertRgba16ToRgba32F(TextureData data)
        {
            if (data.Format != TextureFormat.Rgba16)
                throw new NotSupportedException();

            var result = data.Clone();

            var newData = MemoryBuffer.Create<byte>((data.Width * data.Height * sizeof(float) * 4));

            using var pSrc = result.Content!.MemoryLock();

            using var pDst = newData.MemoryLock();

            EngineNativeLib.ConvertRgba16ToRgba32F((ushort*)pSrc.Data, (float*)pDst.Data, data.Width, data.Height, data.Width * 2);

            result.Content = newData;
            result.Format = TextureFormat.RgbaFloat32;

            return result;

        }

        public static unsafe TextureData ConvertRgb32FToRgba16F(TextureData data)
        {
            if (data.Format != TextureFormat.RgbFloat32)
                throw new NotSupportedException();

            var result = data.Clone();

            var size = data.Width * data.Height * Math.Max(1, data.Depth);

            var newData = MemoryBuffer.Create<byte>(size * 2 * 4);

            using var pSrc = result.Content!.MemoryLock();

            using var pDst = newData.MemoryLock();

            EngineNativeLib.ConvertRgb32FToRgba16F((float*)pSrc.Data, (Half*)pDst.Data, size * 3);

            result.Content = newData;
            result.Format = TextureFormat.RgbaFloat16;

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

                    var floatVector = Avx.ConvertToVector256Single(intVector);

                    floatVector.Store(dstFloat + i);
                }
            }

            for (; i < length; i++)
                dstFloat[i] = srcShort[i];

            return result;
        }

        public static unsafe TextureData DecodeBC(TextureData data)
        {
            var (bcFormat, blockSize) = data.Compression switch
            {
                TextureCompressionFormat.Bc1 => (EngineNativeLib.BcFormat.Bc1, 8),
                TextureCompressionFormat.Bc3 => (EngineNativeLib.BcFormat.Bc3, 16),
                TextureCompressionFormat.Bc7 => (EngineNativeLib.BcFormat.Bc7, 16),
                _ => throw new NotSupportedException($"Unsupported BC format: {data.Compression}")
            };

            var result = data.Clone();
            var depth = Math.Max(1, data.Depth);
            var newData = MemoryBuffer.Create<byte>(data.Width * data.Height * depth * 4);

            using var pSrc = result.Content!.MemoryLock();
            using var pDst = newData.MemoryLock();

            var compressedSliceSize = ((data.Width + 3) / 4) * ((data.Height + 3) / 4) * blockSize;
            var decodedSliceSize = data.Width * data.Height * 4;

            for (var z = 0; z < depth; z++)
            {
                if (!EngineNativeLib.ImageDecodeBC(pSrc.Data + z * compressedSliceSize,
                    (int)data.Width, (int)data.Height, bcFormat, pDst.Data + z * decodedSliceSize))
                    throw new InvalidOperationException("BC decode failed.");
            }

            result.Content = newData;
            
            if (data.Format.IsSrgb())
                result.Format = TextureFormat.SRgba8;
            else
                result.Format = TextureFormat.Rgba8;

            result.Compression = TextureCompressionFormat.Uncompressed;

            return result;
        }

        public static SKBitmap ChangeColorSpace(SKBitmap src, SKColorType dest)
        {
            throw new NotImplementedException("Fix with srgb, does shit");

            //do always for  SKAlphaType.Unpremul
            /*
            if (src.ColorType == dest)
                return src;
            */
            /*
   var newInfo = new SKImageInfo(src.Info.Width, src.Info.Height, dest, SKAlphaType.Unpremul, src.Info.ColorSpace);

   var newBitmap = new SKBitmap(newInfo);

   using var canvas = new SKCanvas(newBitmap);

   canvas.Clear(new SKColor(1, 1, 1, 1));

   using var paint = new SKPaint();
   paint.BlendMode = SKBlendMode.DstOver;

   canvas.DrawBitmap(src, 0, 0, paint);

   src.Dispose();

   return newBitmap;
            */
        }

    }
}
