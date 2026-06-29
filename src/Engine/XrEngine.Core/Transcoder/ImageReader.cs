#pragma warning disable CS0649

using Common.Interop;
using SkiaSharp;
using System.Diagnostics;
using TurboJpeg;

namespace XrEngine
{
    public class ImageReader : BaseTextureLoader
    {
        static readonly string[] Extensions = [".bmp"];

        ImageReader()
        {
        }

        public override IList<TextureData> LoadTexture(Stream stream, TextureLoadOptions? options = null)
        {
            var image = SKBitmap.Decode(stream);

            var outFormat = options?.Format;
            if (outFormat != null)
                image = ImageUtils.ChangeColorSpace(image, ImageUtils.GetSkFormat(outFormat.Value));

            var data = new TextureData
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
