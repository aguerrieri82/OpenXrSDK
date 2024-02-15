#pragma warning disable CS0649

namespace OpenXr.Engine
{
    public class DdsReader : BaseTextureReader
    {
        struct DDS_FILE
        {
            public int magic;
            public DDSHeader header;
        }

        struct DDS_PIXELFORMAT
        {
            public int dwSize;
            public int dwFlags;
            public int dwFourCC;
            public int dwRGBBitCount;
            public int dwRBitMask;
            public int dwGBitMask;
            public int dwBBitMask;
            public int dwABitMask;
        };

        unsafe struct DDSHeader
        {
            public int dwSize;
            public int dwFlags;
            public int dwHeight;
            public int dwWidth;
            public int dwPitchOrLinearSize;
            public int dwDepth;
            public int dwMipMapCount;
            public fixed int dwReserved1[11];
            public DDS_PIXELFORMAT ddspf;
            public int dwCaps;
            public int dwCaps2;
            public int dwCaps3;
            public int dwCaps4;
            public int dwReserved2;
        }

        DdsReader()
        {
        }

        public override unsafe IList<TextureData> Read(Stream stream)
        {
            using var memStream = stream.ToMemory();

            var file = memStream.ReadStruct<DDS_FILE>();

            if (file.magic != 0x20534444)
                throw new InvalidOperationException();

            if (file.header.dwSize != sizeof(DDSHeader))
                throw new InvalidOperationException();

   
            var comp = (TextureCompressionFormat)file.header.ddspf.dwFourCC;
            var format = TextureFormat.Rgb24;

  
            return ReadMips(memStream, (uint)file.header.dwWidth, (uint)file.header.dwHeight, (uint)file.header.dwMipMapCount, comp, format);
        }

        public static readonly DdsReader Instance = new();
    }
}
