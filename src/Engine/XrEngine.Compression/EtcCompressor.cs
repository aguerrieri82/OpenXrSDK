using SkiaSharp;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using XrEngine.Services;

namespace XrEngine.Compression
{
    public static class EtcCompressor
    {
        #region NATIVE

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

        #endregion

        static unsafe SKBitmap CreateImage(TextureData data, SKColorType type)
        {
            var info = new SKImageInfo((int)data.Width, (int)data.Height, type);

            var image = new SKBitmap(info);
            fixed (byte* pDst = image.GetPixelSpan())
            fixed (byte* pSrc = data.Data.Span)
                PackImage(data.Width, data.Height, pSrc, data.Width, data.Height, pDst, ImageUtils.GetPixelSizeByte(type));

            return image;
        }
        public static IList<TextureData> Encode(string fileName, int mipsLevels)
        {
            using var file = File.OpenRead(fileName);
            using var image = SKBitmap.Decode(file);
            return Encode(image, mipsLevels);
        }

        public unsafe static IList<TextureData> Encode(TextureData data, int mipsLevels)
        {
            string? cacheFile = null;   
            if (CachePath != null)
            {
                var hash = BitConverter.ToString(MD5.HashData(data.Data.Span)).Replace("-", "");
                cacheFile = Path.Combine(CachePath, hash + ".pvr");

                if (File.Exists(cacheFile) )
                {
                    using var readStream = File.OpenRead(cacheFile);
                    var cacheData = PvrTranscoder.Instance.LoadTexture(readStream);
                    return cacheData;
                }
            }
   

            var skType = ImageUtils.GetFormat(data.Format);

            bool useSrgb = data.Format == TextureFormat.SRgba32 || data.Format == TextureFormat.SRgb24;

            using var image = CreateImage(data, skType);

            using var bgrImage = ImageUtils.ChangeColorSpace(image, SKColorType.Bgra8888); //TODO investigate, on android rgb is treated as bgr 

            var result = Encode(bgrImage, mipsLevels, useSrgb);

            foreach (var item in result)
            {
                if (mipsLevels == 0)
                    item.MipLevel = data.MipLevel;
                item.Face = data.Face;
            }

            if (cacheFile != null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(cacheFile)!);
                using var writeStream = File.OpenWrite(cacheFile);
                PvrTranscoder.Instance.SaveTexture(writeStream, result);
            }


            return result;
        }

        public unsafe static IList<TextureData> Encode(SKBitmap image, int mipsLevels, bool useSrgb = false)
        {
            var result = new List<TextureData>();

            int level = 0;

            while (true)
            {
                var texData = new TextureData
                {
                    Compression = TextureCompressionFormat.Etc2,
                    MipLevel = (uint)level,
                    Width = (uint)MathF.Max(1, image.Width >> level),
                    Height = (uint)MathF.Max(1, image.Height >> level),
                    Format = useSrgb ? TextureFormat.SRgba32 : TextureFormat.Rgba32
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
                    var pWidth = (int)MathF.Ceiling(texData.Width / 4f) * 4;
                    var pHeight = (int)MathF.Ceiling(texData.Height / 4f) * 4;

                    var so = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);

                    curImage = image.Resize(new SKSizeI((int)texData.Width, (int)texData.Height), so);

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
                        Bgr = image.ColorType == SKColorType.Bgra8888,
                        Linearize = false,
                        UseHeuristics = true,
                        MipMap = false,
                        Format = Format.Pvr,
                        Codec = Type.Etc2_RGBA,
                        Test = 0x11223344
                    };

                    var outData = Encode((uint)curImage.Width, (uint)curImage.Height, pData, ref options, out var outSize);
                    var outDataSpan = new Span<byte>(outData + 52, (int)outSize - 52);

                    texData.Data = outDataSpan.ToArray();
                }

                if (curImage != image)
                    curImage.Dispose();
            }

            result.ForEach(EncodeLevel);

            //Parallel.ForEach(result, EncodeLevel); //TODO fix parallel (crash on android)

            return result;
        }


        public static string? CachePath { get; set; }
    }
}
