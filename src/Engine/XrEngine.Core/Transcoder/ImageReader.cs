#pragma warning disable CS0649

using Common.Interop;
using SkiaSharp;
using System.Diagnostics;
using TurboJpeg;

namespace XrEngine
{
    public class ImageReader : BaseTextureLoader
    {
        static readonly string[] Extensions = [".png", ".jpg", ".bmp"];

        ImageReader()
        {
        }

        public override IList<TextureData> LoadTexture(Stream stream, TextureLoadOptions? options = null)
        {
            if (options?.MimeType == "image/jpeg" && (options?.Format == null || options.Format == TextureFormat.Rgba32))
            {
                byte[] buffer = new byte[stream.Length];
                stream.ReadExactly(buffer);

                TurboJpegLib.ImageData imgData = TurboJpegLib.Decompress(buffer);
                Debug.Assert(imgData.Data != null);

                return [new TextureData
                {
                    Width = (uint)imgData.Width,
                    Height = (uint)imgData.Height,
                    Format = TextureFormat.Rgba32,
                    Data = MemoryBuffer.Create(imgData.Data),
                }];
            }

            SKBitmap image = SKBitmap.Decode(stream);

            TextureFormat? outFormat = options?.Format;
            if (outFormat != null)
                image = ImageUtils.ChangeColorSpace(image, ImageUtils.GetSkFormat(outFormat.Value));


            TextureData data = new TextureData
            {
                Compression = TextureCompressionFormat.Uncompressed,
                Format = ImageUtils.GetFormat(image.ColorType),
                Data = MemoryBuffer.Create(image.Bytes),
                Height = (uint)image.Height,
                Width = (uint)image.Width,
            };

            image.Dispose();

            return [data];
        }

        protected override bool CanHandleExtension(string extension)
        {
            return Extensions.Contains(extension);
        }

        public static readonly ImageReader Instance = new();
    }
}
