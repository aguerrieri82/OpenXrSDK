#pragma warning disable CS0649

using Common.Interop;
using SkiaSharp;
using System.Diagnostics;
using TurboJpeg;
using XrEngine.Tiff;

namespace XrEngine
{
    public class TiffReader : BaseTextureLoader
    {
        static readonly string[] Extensions = [".tif"];

        TiffReader()
        {
        }

        public override unsafe IList<TextureData> LoadTexture(Stream stream, TextureLoadOptions? options = null)
        {
            if (stream is not FileStream fileStream)
                throw new NotSupportedException("Only file streams are supported");

            var tiff = LibTiff.TIFFOpen(fileStream.Name, "r");
            var data = tiff.Read();
            tiff.TIFFClose();

            return [data];
        }

        protected override bool CanHandleExtension(string extension)
        {
            return Extensions.Contains(extension);
        }

        public static readonly TiffReader Instance = new();
    }
}
