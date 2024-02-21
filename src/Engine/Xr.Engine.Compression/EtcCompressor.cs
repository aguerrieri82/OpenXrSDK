using OpenXr.Engine;
using SkiaSharp;
using System.Runtime.InteropServices;

namespace Xr.Engine.Compression
{
    public static class EtcCompressor
    {
        enum Type
        {
            Etc1,
            Etc2_RGB,
            Etc2_RGBA,
            Etc2_R11,
            Etc2_RG11,
            Bc1,
            Bc3,
            Bc4,
            Bc5,
            Bc7
        };

        enum Format
        {
            Pvr = 0,
            Dds = 1
        };

        [StructLayout(LayoutKind.Sequential)]
        struct EncodeOptions
        {
            public bool MipMap;
            public bool Bgr;
            public bool Linearize;
            public Type Codec;
            public Format Format;
            public bool UseHeuristics;
            public bool Dither;
            public int Test;
        };


        [DllImport("etcpack")]
        static unsafe extern void PackImage(uint srcWidth, uint srcHeight, byte* srcData, uint dstWidth, uint dstHeight, byte* dstData, uint pixelSize);


        [DllImport("etcpack")]
        static unsafe extern byte* Encode(
        uint width,
        uint height,
        byte* data,
        ref EncodeOptions options,
        out uint outSize);
        [DllImport("etcpack")]
        static unsafe extern void Free(byte* data);
        public static IList<TextureData> Encode(string fileName, int mipsLevels)
        {
            using var file = File.OpenRead(fileName);   
            using var image = SKBitmap.Decode(file);

            return Encode(image, mipsLevels);
        }

        public unsafe static IList<TextureData> Encode(TextureData data)
        {
            var skType = data.Format switch
            {
                TextureFormat.Rgba32 => SKColorType.Rgba8888,
                TextureFormat.SRgba32 => SKColorType.Srgba8888,
                TextureFormat.Bgra32 => SKColorType.Bgra8888,
                _ => throw new NotSupportedException()
            };

            var image = new SKBitmap((int)data.Width, (int)data.Height, skType, SKAlphaType.Opaque);
            fixed (byte* pDst = image.GetPixelSpan())
            fixed (byte* pSrc = data.Data)
                PackImage(data.Width, data.Height, pSrc, data.Width, data.Height, pDst, 4);

            return Encode(image, 0);
        }

        public unsafe static IList<TextureData> Encode(SKBitmap image, int mipsLevels)
        {
            var result = new List<TextureData>();

            int level = 0;

            while (true)
            {
                var texData = new TextureData
                {
                    Compression = TextureCompressionFormat.Etc2,
                    MipLevel = (uint)level,
                    Width = (uint)Math.Max(1, image.Width >> level),
                    Height = (uint)Math.Max(1, image.Height >> level),
                    Format = TextureFormat.Rgba32
                };

                result.Add(texData);

                if (level >= mipsLevels || texData.Width <= 4 || texData.Height <= 4)
                    break;

                level++;
            }

            unsafe void EncodeLevel(TextureData texData)
            {
                SKBitmap curImage;

                if (texData.Width != image.Width || texData.Height != image.Height)
                {
                    var pWidth = (int)Math.Ceiling(texData.Width / 4f) * 4;
                    var pHeight = (int)Math.Ceiling(texData.Height / 4f) * 4;

                    curImage = image.Resize(new SKSizeI((int)texData.Width, (int)texData.Height), SKSamplingOptions.Default);

                    if (pWidth != texData.Width || pHeight != texData.Height)
                    {
                        var newImage = new SKBitmap(new SKImageInfo
                        {
                            Width = pWidth,
                            Height = pHeight,
                            AlphaType = curImage.AlphaType,
                            ColorSpace = curImage.ColorSpace,
                            ColorType = curImage.ColorType
                        });

                        fixed (byte* pSrc = curImage.GetPixelSpan())
                        fixed (byte* pDst = newImage.GetPixelSpan())
                            PackImage(
                                (uint)curImage.Width,
                                (uint)curImage.Height,
                                pSrc,
                                (uint)newImage.Width,
                                (uint)newImage.Height,
                                pDst,
                                (uint)newImage.BytesPerPixel);

                        curImage.Dispose();
                        curImage = newImage;
                    }
                }
                else
                    curImage = image;


                fixed (byte* pData = curImage.GetPixelSpan())
                {
                    var options = new EncodeOptions()
                    {
                        Bgr = false,
                        Linearize = true,
                        UseHeuristics = true,
                        MipMap = false,
                        Format = Format.Pvr,
                        Codec = Type.Etc2_RGBA,
                        Test = (int)0x11223344
                    };

                    var outData = Encode((uint)curImage.Width, (uint)curImage.Height, (byte*)pData, ref options, out var outSize);
                    var outDataSpan = new Span<byte>(outData + 52, (int)outSize - 52);

                    texData.Data = outDataSpan.ToArray();
                }

                if (curImage != image)
                    curImage.Dispose();

            }

            result.ForEach(EncodeLevel);

            //Parallel.ForEach(result, EncodeLevel);

            return result;
        }
    }
}
