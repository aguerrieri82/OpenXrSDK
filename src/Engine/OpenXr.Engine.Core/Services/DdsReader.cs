using OpenXr.Engine.Abstraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{

    public class DdsReader : ITextureReader
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

        public static uint FixEtc2Size(uint value)
        {
            return (uint)((value + 3) & ~3);
        }

        public unsafe TextureData Read(Stream stream)
        {
            using var memStream = stream.ToMemory();

            var file = memStream.ReadStruct<DDS_FILE>();
            
            if (file.magic != 0x20534444)
                throw new Exception();
            
            if (file.header.dwSize != sizeof(DDSHeader))
                throw new Exception();

            var result = new TextureData();
            result.Width = (uint)file.header.dwWidth;
            result.Height = (uint)file.header.dwHeight;
            result.Compression = (TextureCompressionFormat)file.header.ddspf.dwFourCC;

            if (result.Compression == TextureCompressionFormat.Etc2)
            {
                result.Format = TextureFormat.Rgb24;
                result.Width = FixEtc2Size(result.Width);
                result.Height = FixEtc2Size(result.Height);
            }

            result.Data = new byte[memStream.Length - memStream.Position];
            memStream.Read(result.Data);

            return result;
        }

        public static readonly DdsReader Instance = new DdsReader();
    }
}
