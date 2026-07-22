using Common.Interop;

namespace XrEngine
{
    public class TextureRegion
    {
        public TextureRegion()
        {
            Depth = 1;
        }

        public int X;

        public int Y;

        public int Z;

        public uint Width;

        public uint Height;

        public uint Depth;

        public uint MipLevel;

        public uint Layer;

        public TextureFormat Format;

        public IMemoryBuffer<byte>? Data;
    }

}
