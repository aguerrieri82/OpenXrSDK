#pragma warning disable CS0649

using System.Runtime.InteropServices;

namespace Xr.Engine
{

    public class PvrDecoder : BaseTextureReader
    {
        const uint Version = 0x03525650;

        enum PixelFormat : ulong
        {
            ETC1 = 6,
            ETC2_RGB = 22,
            ETC2_RGBA = 23,
            ETC2_RGB_A1 = 24,
        }

        public enum ColourSpace : uint
        {
            LinearRGB = 0,
            sRGB = 1,
            Float = 12
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
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

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        unsafe struct PvrMeta
        {
            public uint FourCC;
            public uint Key;
            public uint DataSize;

        }


        PvrDecoder()
        {
        }

        public unsafe void Write(Stream stream, IList<TextureData> images)
        {
            var header = new PvrHeader();
            header.Version = Version;
            header.Width = images[0].Width;
            header.Height = images[0].Height;
            header.NumSurfaces = 1;
            header.NumFaces = 1;
            header.Depth = 1;
            header.MIPMapCount = (uint)images.Count;

            if (images[0].Compression == TextureCompressionFormat.Etc2)
            {
                switch (images[0].Format)
                {
                    case TextureFormat.Rgba32:
                        header.ColourSpace = ColourSpace.LinearRGB;
                        header.PixelFormat = PixelFormat.ETC2_RGBA;
                        break;
                    case TextureFormat.Rgb24:
                        header.ColourSpace = ColourSpace.LinearRGB;
                        header.PixelFormat = PixelFormat.ETC2_RGB;
                        break;
                    case TextureFormat.SRgb24:
                        header.ColourSpace = ColourSpace.sRGB;
                        header.PixelFormat = PixelFormat.ETC2_RGB;
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }
            else
                throw new NotSupportedException();

            stream.WriteStruct(header);
            foreach (var img in images)
                stream.Write(img.Data);

            stream.Dispose();
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
                case PixelFormat.ETC1:
                    comp = TextureCompressionFormat.Etc1;
                    format = TextureFormat.Rgb24;
                    break;
                default:
                    throw new NotSupportedException();
            }

            return ReadMips(memStream, header.Width, header.Height, header.MIPMapCount, comp, format);


        }

        public static readonly PvrDecoder Instance = new();
    }
}
