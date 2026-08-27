#pragma warning disable CS0649

using Common.Interop;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using static XrEngine.EngineNativeLib;

namespace XrEngine
{

    public class Ktx2Reader : BaseTextureLoader
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
            public ulong sgdByteOffset;
            public ulong sgdByteLength;
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
            VK_FORMAT_UNDEFINED = 0,
            VK_FORMAT_R8G8B8_USCALED = 25,
            VK_FORMAT_R8G8B8A8_USCALED = 39,
            VK_FORMAT_R16G16B16A16_SFLOAT = 97,
        }

        Ktx2Reader()
        {
        }

        public override unsafe IList<TextureData> LoadTexture(Stream stream, TextureLoadOptions? options = null)
        {
            using var seekStream = stream.EnsureSeek();
            var header = seekStream.ReadStruct<KtxHeader>();
            var magic = Encoding.ASCII.GetString(new Span<byte>(header.identifier, 12));
            if (!magic.Contains("KTX 20"))
                throw new NotSupportedException();

            if (header.vkFormat == VkFormat.VK_FORMAT_UNDEFINED)
            {
                seekStream.Position = 0;
                var data = new byte[seekStream.Length];
                seekStream.ReadExactly(data);
                fixed (void* pData = data)
                {
                    var targetFormat = OperatingSystem.IsAndroid()
                        ? BasisTextureFormat.AstcLdr4x4Rgba
                        : BasisTextureFormat.Rgba32;

                    var result = new List<TextureData>();
                    
                    lock (this)
                    {
                        BasisTranscodeKtx2(pData, (uint)data.Length, targetFormat, out var basisTexture);

                        Debug.Assert(basisTexture.ImageCount > 0);

                        for (var i = 0; i < basisTexture.ImageCount; i++)
                        {
                            var image = basisTexture.Images[i];

                            result.Add(new TextureData
                            {
                                Width = image.Width,
                                Height = image.Height,
                                Depth = 1,
                                MipLevel = image.Level,
                                Layer = image.Layer,
                                Format = TextureFormat.Rgba8,
                                Compression = targetFormat == BasisTextureFormat.AstcLdr4x4Rgba
                                    ? TextureCompressionFormat.Astc
                                    : TextureCompressionFormat.Uncompressed,
                                Content = MemoryBuffer.Create(new Span<byte>(image.Data, (int)image.Size)),
                                BlockSize = 16
                            });
                        }

                        BasisFreeTexture(ref basisTexture);
                    }

                    return result;
                }
                    
            }

            if (header.supercompressionScheme != CompressionScheme.None ||
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
                    format = TextureFormat.Rgb8;
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
                seekStream.Position = (int)header.sgdByteOffset;

            return ReadData(seekStream, header.pixelWidth, header.pixelHeight, 1, header.levelCount, header.faceCount, comp, format);
        }

        protected override bool CanHandleExtension(string extension)
        {
            return extension == ".ktx2";
        }

        public static readonly Ktx2Reader Instance = new();
    }
}
