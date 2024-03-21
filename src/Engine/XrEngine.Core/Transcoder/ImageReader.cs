﻿#pragma warning disable CS0649

using SkiaSharp;
using System.Runtime.InteropServices;
using static System.Net.Mime.MediaTypeNames;

namespace XrEngine
{
    public class ImageReader : BaseTextureReader
    {
        ImageReader()
        {
        }


        public override unsafe IList<TextureData> Read(Stream stream, TextureReadOptions? options = null)
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

        public static readonly ImageReader Instance = new();
    }
}
