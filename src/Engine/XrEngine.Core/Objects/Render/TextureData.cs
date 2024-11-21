using Common.Interop;

namespace XrEngine
{

    public class TextureData
    {
        public uint Width { get; set; }

        public uint Height { get; set; }

        public uint Depth { get; set; }

        public uint MipLevel { get; set; }

        public uint Face { get; set; }

        public TextureFormat Format { get; set; }

        public TextureCompressionFormat Compression { get; set; }

        public IMemoryBuffer<byte>? Data { get; set; }

    }
}
