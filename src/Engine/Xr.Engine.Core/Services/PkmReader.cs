#pragma warning disable CS0649

using System.Runtime.InteropServices;
using System.Text;

namespace OpenXr.Engine
{
    public class PkmReader : ITextureReader
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

        public unsafe IList<TextureData> Read(Stream stream)
        {
            using var memStream = stream.ToMemory();

            var header = memStream.ReadStruct<Etc2Header>();

            var result = new TextureData();
            var magic = Encoding.ASCII.GetString(new Span<byte>(header.magic, 6));
            if (magic != "PKM 20")
                throw new InvalidOperationException();

            result.Width = Invert(header.encodedWidth);
            result.Height = Invert(header.encodedHeight);
            result.Data = new byte[memStream.Length - memStream.Position];
            result.Compression = TextureCompressionFormat.Etc2;
            result.Format = TextureFormat.SRgb24;

            memStream.Read(result.Data);

            return [result];
        }

        public static readonly PkmReader Instance = new();
    }
}
