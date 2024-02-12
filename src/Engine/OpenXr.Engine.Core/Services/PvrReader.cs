using System;
using static OpenXr.Engine.KtxReader;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace OpenXr.Engine
{

    public class PvrReader : BaseTextureReader
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

        PvrReader()
        {
        }

        public override unsafe IList<TextureData> Read(Stream stream)
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

            TextureCompressionFormat comp;
            TextureFormat format;

            switch (header.PixelFormat)
            {
                case PixelFormat.ETC2_RGB:
                    comp = TextureCompressionFormat.Etc2;
                    if (header.ColourSpace == ColourSpace.sRGB)
                        format = TextureFormat.SRgb24;
                    else
                        format = TextureFormat.Rgb24;
                    break;
                case PixelFormat.ETC2_RGBA:
                    comp = TextureCompressionFormat.Etc2;
                    format = TextureFormat.Rgba32;
                    break;
                default:
                    throw new NotSupportedException();
            }

            return ReadMips(stream, header.Width, header.Height, header.MIPMapCount, comp, format);
        }

        public static readonly PvrReader Instance = new();
    }
}
