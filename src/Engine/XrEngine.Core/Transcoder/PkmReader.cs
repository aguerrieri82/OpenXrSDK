#pragma warning disable CS0649

using System.Runtime.InteropServices;
using System.Text;

namespace XrEngine
{
    public class PkmReader : BaseTextureLoader
    {

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        unsafe struct Etc2Header
        {
            public fixed byte magic[6];
            public short format;
            public ushort encodedWidth;
            public ushort encodedHeight;
            public ushort width;
            public ushort height;
        }

        protected static ushort Invert(ushort value)
        {
            return (ushort)(value >> 8 | ((value << 8) & 0xFF00));
        }

        public override unsafe IList<TextureData> LoadTexture(Stream stream, TextureLoadOptions? options = null)
        {
            using var seekStream = stream.EnsureSeek();

            var header = seekStream.ReadStruct<Etc2Header>();

            var result = new TextureData();
            var magic = Encoding.ASCII.GetString(new Span<byte>(header.magic, 6));
            if (magic != "PKM 20")
                throw new InvalidOperationException();

            result.Width = Invert(header.encodedWidth);
            result.Height = Invert(header.encodedHeight);
            result.Data = MemoryBuffer.Create<byte>((uint)(seekStream.Length - seekStream.Position));
            result.Compression = TextureCompressionFormat.Etc2;
            result.Format = TextureFormat.SRgb24;

            seekStream.ReadExactly(result.Data.AsSpan());

            return [result];
        }

        protected override bool CanHandleExtension(string extension)
        {
            return extension == ".pkm";
        }

        public static readonly PkmReader Instance = new();
    }
}
