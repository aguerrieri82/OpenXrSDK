namespace XrEngine
{

    public class TextureData
    {
        public uint Width { get; set; }

        public uint Height { get; set; }

        public uint MipLevel { get; set; }

        public TextureFormat Format { get; set; }

        public TextureCompressionFormat Compression { get; set; }

        public byte[]? Data { get; set; }
    }
}
