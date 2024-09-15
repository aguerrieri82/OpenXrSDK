﻿#pragma warning disable CS0649

using SkiaSharp;

namespace XrEngine
{
    public class ImageReader : BaseTextureLoader
    {
        static readonly string[] Extensions = [".png", ".jpg", ".bmp", ".tif"];

        ImageReader()
        {
        }

        public override unsafe IList<TextureData> LoadTexture(Stream stream, TextureLoadOptions? options = null)
        {
            var image = SKBitmap.Decode(stream);

            var outFormat = options?.Format;
            if (outFormat != null)
                image = ImageUtils.ChangeColorSpace(image, ImageUtils.GetFormat(outFormat.Value));


            var data = new TextureData
            {
                Compression = TextureCompressionFormat.Uncompressed,
                Format = ImageUtils.GetFormat(image.ColorType),
                Data = image.GetPixelSpan().ToArray(),
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
