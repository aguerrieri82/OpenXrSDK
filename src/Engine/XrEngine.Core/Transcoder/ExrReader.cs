#pragma warning disable CS0649

using SharpEXR;

namespace XrEngine
{
    public class ExrReader : BaseTextureLoader
    {

        ExrReader()
        {
        }

        public unsafe override IList<TextureData> LoadTexture(Stream stream, TextureLoadOptions? options = null)
        {
            var exrFile = EXRFile.FromStream(stream);
            var part = exrFile.Parts[0];

            part.Open(stream);

            var floats = part.GetFloats(ChannelConfiguration.RGB, true, GammaEncoding.Linear, true);

            var data = new TextureData
            {
                Compression = TextureCompressionFormat.Uncompressed,
                Format = TextureFormat.RgbFloat32,
                Data = new Span<byte>((byte*)&floats, floats.Length * 4).ToArray(),
                Height = (uint)part.Header.DataWindow.Height,
                Width = (uint)part.Header.DataWindow.Width,
            };

            return [data];
        }

        protected override bool CanHandleExtension(string extension)
        {
            return extension == ".exr";
        }

        public static readonly ExrReader Instance = new();
    }
}
