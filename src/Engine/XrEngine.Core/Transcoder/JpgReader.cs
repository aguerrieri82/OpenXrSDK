using Common.Interop;
using System.Diagnostics;
using TurboJpeg;

namespace XrEngine.Transcoder
{
    public class JpgReader : BaseTextureLoader
    {
        JpgReader()
        {
        }

        public override IList<TextureData> LoadTexture(Stream stream, TextureLoadOptions? options = null)
        {
            var buffer = new byte[stream.Length];
            stream.ReadExactly(buffer);

            var imgData = TurboJpegLib.Decompress(buffer);
            Debug.Assert(imgData.Data != null);

            var isSrgb = options?.IsSrgb ?? false;

            return [new TextureData
            {
                Width = (uint)imgData.Width,
                Height = (uint)imgData.Height,
                Format = isSrgb ? TextureFormat.SRgba8 : TextureFormat.Rgba8,
                Content = MemoryBuffer.Create(imgData.Data),
            }];
        }

        protected override bool CanHandleExtension(string extension)
        {
            return extension == ".jpg";
        }

        public static readonly JpgReader Instance = new();
    }
}
