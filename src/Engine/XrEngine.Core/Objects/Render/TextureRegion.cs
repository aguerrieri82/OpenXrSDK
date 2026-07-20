using Common.Interop;

namespace XrEngine
{
    public class TextureRegion
    {
        public TextureRegion()
        {
            Depth = 1;
        }

        public int X { get; set; }

        public int Y { get; set; }

        public int Z { get; set; }

        public uint Width { get; set; }

        public uint Height { get; set; }

        public uint Depth { get; set; }

        public uint MipLevel { get; set; }

        public uint Layer { get; set; }

        public TextureFormat Format { get; set; }

        public IMemoryBuffer<byte>? Data { get; set; }
    }

}
