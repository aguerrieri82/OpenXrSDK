#pragma warning disable CS0649

using System.Runtime.InteropServices;
using System.Text;

namespace XrEngine
{

    public class Ktx2Reader : BaseTextureReader
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        unsafe struct KtxHeader
        {
            public fixed byte identifier[12];
            public VkFormat vkFormat;
            public uint typeSize;
            public uint pixelWidth;
            public uint pixelHeight;
            public uint pixelDepth;
            public uint layerCount;
            public uint faceCount;
            public uint levelCount;
            public CompressionScheme supercompressionScheme;

            // Index 
            public uint dfdByteOffset;
            public uint dfdByteLength;
            public uint kvdByteOffset;
            public uint kvdByteLength;
            public uint sgdByteOffset;
            public uint sgdByteLength;
        }

        public enum CompressionScheme : uint
        {
            None = 0,
            BasisLZ = 1,
            Zstandard = 2,
            ZLIB = 3
        }

        public enum VkFormat : uint
        {
            VK_FORMAT_R8G8B8_USCALED = 25,
            VK_FORMAT_R8G8B8A8_USCALED = 39,
            VK_FORMAT_R16G16B16A16_SFLOAT = 97,
        }

        Ktx2Reader()
        {
        }

        public override unsafe IList<TextureData> Read(Stream stream)
        {
            using var seekStream = stream.EnsureSeek();
            var header = seekStream.ReadStruct<KtxHeader>();
            var magic = Encoding.ASCII.GetString(new Span<byte>(header.identifier, 12));
            if (!magic.Contains("KTX 20"))
                throw new NotSupportedException();

            if (header.supercompressionScheme !=  CompressionScheme.None ||
                header.pixelDepth != 0 ||
                header.layerCount != 0)
            {
                throw new NotSupportedException();
            }


            TextureCompressionFormat comp;
            TextureFormat format;

            switch (header.vkFormat)
            {
                case VkFormat.VK_FORMAT_R8G8B8_USCALED:
                    comp = TextureCompressionFormat.Uncompressed;
                    format = TextureFormat.Rgb24;
                    break;
                case VkFormat.VK_FORMAT_R16G16B16A16_SFLOAT:
                    comp = TextureCompressionFormat.Uncompressed;
                    format = TextureFormat.RgbaFloat16;
                    break;
                default:
                    throw new NotSupportedException();
            }


            if (header.sgdByteOffset == 0)
                seekStream.Position = header.kvdByteOffset + header.kvdByteLength;
            else
                seekStream.Position = header.sgdByteOffset;

            return ReadData(seekStream, header.pixelWidth, header.pixelHeight, header.levelCount, header.faceCount, comp, format);
        }

        public static readonly Ktx2Reader Instance = new();
    }
}
