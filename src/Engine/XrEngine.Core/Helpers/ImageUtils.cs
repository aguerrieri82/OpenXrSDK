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
        static readonly Dictionary<SKColorType, TextureFormat> SKIA_FORMAT_MAP = new() {
            { SKColorType.Bgra8888, TextureFormat.Bgra32 },
            { SKColorType.Rgba8888, TextureFormat.Rgba32 },
            { SKColorType.Srgba8888, TextureFormat.SRgba32 },
            { SKColorType.Gray8, TextureFormat.GrayInt8 },
            { SKColorType.RgbaF16, TextureFormat.RgbaFloat16 },
            { SKColorType.RgbaF32, TextureFormat.RgbaFloat32 },

        };

        public static SKBitmap ApplyGaussianBlur(SKBitmap bitmap, float radius)
        {
            using SKSurface surface = SKSurface.Create(new SKImageInfo(bitmap.Width, bitmap.Height));

            using SKPaint paint = new SKPaint
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
            IMemoryBuffer<byte> mrImage = MemoryBuffer.Create<byte>(metal.Width * metal.Height * 4);

            using MemoryLock<byte> pMetal = metal.Data!.MemoryLock();
            using MemoryLock<byte> pRough = roughness.Data!.MemoryLock();
            using MemoryLock<byte> pDst = mrImage.MemoryLock();
            EngineNativeLib.ImageCopyChannel(pMetal, pDst, metal.Width, metal.Height, metal.Width * GetPixelSizeByte(metal.Format), metal.Width * 4, 0, 2, 1);
            EngineNativeLib.ImageCopyChannel(pRough, pDst, roughness.Width, roughness.Height, roughness.Width * GetPixelSizeByte(roughness.Format), metal.Width * 4, 0, 1, 1);

            Texture2D tex = new Texture2D
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
            IMemoryBuffer<byte> metalData = MemoryBuffer.Create<byte>(roughness.Width * roughness.Height * 1);
            metalData.AsSpan().Fill(255);

            TextureData texData = new TextureData
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
            return SKIA_FORMAT_MAP.First(a => a.Value == format).Key;
        }

        public static TextureFormat GetFormat(SKColorType color)
        {
            if (!SKIA_FORMAT_MAP.TryGetValue(color, out TextureFormat format))
                throw new NotSupportedException();
            return format;
        }

        public static SKBitmap? ToBitmap(TextureData data, bool flipY)
        {
            if (data.Height == 0 || data.Width == 0)
                return null;

            uint pixelSize = GetPixelSizeByte(data.Format);

            SKBitmap image = new SKBitmap((int)data.Width, (int)data.Height, GetSkFormat(data.Format), SKAlphaType.Opaque);

            Debug.Assert(data.Data != null);

            Debug.Assert(image.RowBytes == data.Width * pixelSize);

            if (flipY)
            {
                IMemoryBuffer<byte> dst = MemoryBuffer.Create<byte>(data.Height * data.Width * pixelSize);

                using MemoryLock<byte> pData = data.Data.MemoryLock();
                using MemoryLock<byte> pDst = dst.MemoryLock();
                EngineNativeLib.ImageFlipY(pData, pDst, data.Width, data.Height, data.Width * pixelSize);
                image.SetPixels(pDst);
            }
            else
            {
                using MemoryLock<byte> pData = data.Data.MemoryLock();
                image.SetPixels(pData);
            }

            return image;
        }

        public static unsafe IMemoryBuffer<byte> ConvertShortToFloat(IMemoryBuffer<byte> data)
        {
            int i = 0;
            int length = (int)data.Size / 2;
            int vectorSize = Vector128<short>.Count;

            IMemoryBuffer<byte> result = MemoryBuffer.Create<byte>((uint)length * sizeof(float));

            using MemoryLock<byte> src = data.MemoryLock();
            using MemoryLock<byte> dst = result.MemoryLock();

            float* dstFloat = (float*)dst.Data;
            short* srcShort = (short*)src.Data;

            if (Avx2.IsSupported)
            {
                for (; i <= length - vectorSize; i += vectorSize)
                {
                    Vector128<short> shortVector = Unsafe.Read<Vector128<short>>(srcShort + i);

                    Vector256<int> intVector = Avx2.ConvertToVector256Int32(shortVector);

                    Vector256<float> floatVector = Avx2.ConvertToVector256Single(intVector);

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

            SKImageInfo newInfo = new SKImageInfo(src.Info.Width, src.Info.Height, dest, SKAlphaType.Unpremul, src.Info.ColorSpace);

            SKBitmap newBitmap = new SKBitmap(newInfo);

            using SKCanvas canvas = new SKCanvas(newBitmap);

            canvas.Clear(new SKColor(1, 1, 1, 1));

            using SKPaint paint = new SKPaint();
            paint.BlendMode = SKBlendMode.DstOver;

            canvas.DrawBitmap(src, 0, 0, paint);

            src.Dispose();

            return newBitmap;
        }

    }
}
