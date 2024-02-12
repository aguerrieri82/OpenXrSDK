using System;
using static OpenXr.Engine.KtxReader;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace OpenXr.Engine
{

    public class PvrReader : ITextureReader
    {
        const uint Version = 0x03525650;

        enum PixelFormat : ulong {
            ETC2_RGB = 22,
            ETC2_RGBA = 23,
            ETC2_RGB_A1 = 24,
        }

        public enum ColourSpace :uint
        {
            LinearRGB = 0,
            sRGB = 1,
            Float = 12
        }

        unsafe struct PvrHeader
        {

            public uint Version;
            public uint Flags;
            public PixelFormat PixelFormat;
            public ColourSpace ColourSpace;
            public uint ChannelType;
            public uint Height;
            public uint Width;
            public uint Depth;
            public uint NumSurfaces;
            public uint NumFaces;
            public uint MIPMapCount;
            public uint MetaDataSize;
        }

        unsafe struct PvrMeta
        {
            public uint FourCC;
            public uint Key;
            public uint DataSize;

        }

        public static uint FixEtc2Size(uint value)
        {
            return (uint)((value + 3) & ~3);
        }

        public unsafe IList<TextureData> Read(Stream stream)
        {
            using var memStream = stream.ToMemory();

            var header = memStream.ReadStruct<PvrHeader>();

            if (header.Version != Version)
                throw new InvalidOperationException();

            if (header.MetaDataSize > 0)
            {
                var meta = memStream.ReadStruct<PvrMeta>();
                if (meta.DataSize > 0)
                    memStream.Position += (header.MetaDataSize - sizeof(PvrMeta));
            }


            var test = (ulong)header.PixelFormat >> 32;

            if (header.NumSurfaces != 1 ||
                header.NumFaces != 1 ||
                header.Depth != 1)
            {
                throw new NotSupportedException();
            }



            uint bitPerPixel = 0;
            TextureCompressionFormat comp;
            TextureFormat format;

            uint Align(uint value, uint align)
            {
                return (uint)Math.Ceiling(value / (float)align) * align;
            }


            switch (header.PixelFormat)
            {
                case PixelFormat.ETC2_RGB:
                    comp = TextureCompressionFormat.Etc2;
                    format = TextureFormat.Rgb24;
                    bitPerPixel = 4;
                    break;
                case PixelFormat.ETC2_RGBA:
                    comp = TextureCompressionFormat.Etc2;
                    format = TextureFormat.Rgba32;
                    bitPerPixel = 8;
                    break;
                default:
                    throw new NotSupportedException();
            }


            var results = new List<TextureData>(); 

            for (var i = 0; i < header.MIPMapCount; i++)
            {
                var item = new TextureData
                {
                    Width = Math.Max(1, header.Width >> i),
                    Height = Math.Max(1, header.Height >> i),
                    MipLevel = (uint)i,
                    Format = format,
                    Compression = comp, 
                };

                var size = (uint)(Math.Ceiling(item.Width / 4f) * Math.Ceiling(item.Height / 4f) * 8);

                item.Data = new byte[size];

                var totRead = memStream.Read(item.Data);
                if (totRead != item.Data.Length)
                    break;

                results.Add(item);
            }

            return results;
        }

        public static readonly PvrReader Instance = new PvrReader();
    }
}
