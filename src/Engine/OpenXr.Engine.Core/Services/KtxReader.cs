using System.Diagnostics;
using System.Text;

namespace OpenXr.Engine
{

    public class KtxReader : BaseTextureReader
    {
        unsafe struct KtxHeader
        {
            public fixed byte identifier[12];
            public uint endianness;
            public uint glType;
            public uint glTypeSize;
            public uint glFormat;
            public GlInternalFormat glInternalFormat;
            public uint glBaseInternalFormat;
            public uint pixelWidth;
            public uint pixelHeight;
            public uint pixelDepth;
            public uint numberOfArrayElements;
            public uint numberOfFaces;
            public uint numberOfMipmapLevels;
            public uint bytesOfKeyValueData;
        }

        public enum GlInternalFormat : uint
        {
            CompressedRgb8Etc2 = 37492,
            CompressedRgb8Etc2Oes = 37492,
            CompressedSrgb8Etc2 = 37493,
            CompressedSrgb8Etc2Oes = 37493,
            CompressedRgb8PunchthroughAlpha1Etc2 = 37494,
            CompressedRgb8PunchthroughAlpha1Etc2Oes = 37494,
            CompressedSrgb8PunchthroughAlpha1Etc2 = 37495,
            CompressedSrgb8PunchthroughAlpha1Etc2Oes = 37495,
            CompressedRgba8Etc2Eac = 37496,
            CompressedRgba8Etc2EacOes = 37496,
            CompressedSrgb8Alpha8Etc2Eac = 37497,
            CompressedSrgb8Alpha8Etc2EacOes = 37497,
        }

        KtxReader()
        {
        }

        public override unsafe IList<TextureData> Read(Stream stream)
        {
            using var memStream = stream.ToMemory();
            var header = memStream.ReadStruct<KtxHeader>();
            var magic = Encoding.ASCII.GetString(new Span<byte>(header.identifier, 12));
            if (!magic.Contains("KTX 11"))
                throw new NotSupportedException();

            if (header.bytesOfKeyValueData > 0 ||
                header.numberOfArrayElements != 0 ||
                header.pixelDepth != 0 ||
                header.numberOfFaces != 1)
            {
                throw new NotSupportedException();
            }

            var imageSize = memStream.ReadStruct<uint>();

            TextureCompressionFormat comp;
            TextureFormat format;

            switch (header.glInternalFormat)
            {
                case GlInternalFormat.CompressedRgb8Etc2:
                    comp = TextureCompressionFormat.Etc2;
                    format = TextureFormat.Rgb24;
                    break;
                default:
                    throw new NotSupportedException();
            }

            return ReadMips(stream, header.pixelWidth, header.pixelHeight, header.numberOfMipmapLevels, comp, format);
        }

        public static readonly KtxReader Instance = new();
    }
}
