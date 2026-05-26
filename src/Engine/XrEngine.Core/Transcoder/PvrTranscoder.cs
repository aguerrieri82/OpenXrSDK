#pragma warning disable CS0649

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace XrEngine
{

    public class PvrTranscoder : BaseTextureLoader, ITextureWriter
    {
        const uint Version = 0x03525650;

        enum PixelFormat : ulong
        {
            ETC1 = 6,
            ETC2_RGB = 22,
            ETC2_RGBA = 23,
            ETC2_RGB_A1 = 24,
            ASTC_4x4 = 27,
            ASTC_5x4 = 28,
            ASTC_5x5 = 29,
            ASTC_6x5 = 30,
            ASTC_6x6 = 31,
            ASTC_8x5 = 32,
            ASTC_8x6 = 33,
            ASTC_8x8 = 34,
            ASTC_10x5 = 35,
            ASTC_10x6 = 36,
            ASTC_10x8 = 37,
            ASTC_10x10 = 38,
            ASTC_12x10 = 39,
            ASTC_12x12 = 40,
            RGBA8 = 0x0808080861626772,
            RGB8 = 0x0008080800626772,
            RGBFloat32 = 0x0020202000626772,
            RGBAFloat32 = 0x2020202061626772,
            RGBAFloat16 = 0x1010101061626772
        }

        public enum ColorSpace : uint
        {
            LinearRGB = 0,
            sRGB = 1,
            Float = 12
        }

        public enum ChannelType : uint
        {
            UnsignedByteNormalized = 0,
            Float = 12
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        unsafe struct PvrHeader
        {
            public uint Version;
            public uint Flags;
            public PixelFormat PixelFormat;
            public ColorSpace ColorSpace;
            public ChannelType ChannelType;
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

        PvrTranscoder()
        {
        }

        public void SaveTexture(Stream stream, IList<TextureData> images)
        {

            var header = new PvrHeader
            {
                Version = Version,
                Width = images[0].Width,
                Height = images[0].Height,
                NumSurfaces = 1,
                NumFaces = images.Count == 1 ? 1 : images.Max(a => a.Face) + 1,
                Depth = 1,
                MIPMapCount = images.Count == 1 ? 1 : images.Max(a => a.MipLevel) + 1
            };

            if (images[0].Compression == TextureCompressionFormat.Astc)
            {
                header.PixelFormat = images[0].BlockSize switch
                {
                    4 => PixelFormat.ASTC_4x4,
                    6 => PixelFormat.ASTC_6x6,
                    8 => PixelFormat.ASTC_8x8,
                    _ => throw new NotSupportedException()
                };
                if (images[0].Format.IsSrgb())
                    header.ColorSpace = ColorSpace.sRGB;
                else
                    header.ColorSpace = ColorSpace.LinearRGB;
            }
            else
            {
                switch (images[0].Format)
                {
                    case TextureFormat.Rgba32:
                        header.ColorSpace = ColorSpace.LinearRGB;
                        if (images[0].Compression == TextureCompressionFormat.Etc2)
                            header.PixelFormat = PixelFormat.ETC2_RGBA;
                        else
                            header.PixelFormat = PixelFormat.RGBA8;
                        break;
                    case TextureFormat.Rgb24:
                        header.ColorSpace = ColorSpace.LinearRGB;
                        if (images[0].Compression == TextureCompressionFormat.Etc2)
                            header.PixelFormat = PixelFormat.ETC2_RGB;
                        else
                            header.PixelFormat = PixelFormat.RGB8;
                        break;
                    case TextureFormat.SRgb24:
                        header.ColorSpace = ColorSpace.sRGB;
                        if (images[0].Compression == TextureCompressionFormat.Etc2)
                            header.PixelFormat = PixelFormat.ETC2_RGB;
                        else
                            header.PixelFormat = PixelFormat.RGB8;
                        break;
                    case TextureFormat.SRgba32:
                        header.ColorSpace = ColorSpace.sRGB;
                        if (images[0].Compression == TextureCompressionFormat.Etc2)
                            header.PixelFormat = PixelFormat.ETC2_RGBA;
                        else
                            header.PixelFormat = PixelFormat.RGBA8;
                        break;
                    case TextureFormat.RgbFloat32:
                        header.ColorSpace = ColorSpace.LinearRGB;
                        header.PixelFormat = PixelFormat.RGBFloat32;
                        header.ChannelType = ChannelType.Float;
                        break;
                    case TextureFormat.RgbaFloat32:
                        header.ColorSpace = ColorSpace.LinearRGB;
                        header.PixelFormat = PixelFormat.RGBAFloat32;
                        header.ChannelType = ChannelType.Float;
                        break;
                    case TextureFormat.RgbaFloat16:
                        header.ColorSpace = ColorSpace.LinearRGB;
                        header.PixelFormat = PixelFormat.RGBAFloat16;
                        header.ChannelType = ChannelType.Float;
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }

            stream.WriteStruct(header);

            foreach (var img in images.OrderBy(a => a.MipLevel).ThenBy(a => a.Face))
            {
                Debug.Assert(img.Data != null);
                stream.Write(img.Data.AsSpan());
            }

            stream.Dispose();
        }


        public override unsafe IList<TextureData> LoadTexture(Stream stream, TextureLoadOptions? options = null)
        {
            using var seekStream = stream.EnsureSeek();

            var header = seekStream.ReadStruct<PvrHeader>();

            if (header.Version != Version)
                throw new InvalidOperationException();

            if (header.MetaDataSize > 0)
            {
                var meta = seekStream.ReadStruct<PvrMeta>();
                if (meta.DataSize > 0)
                    seekStream.Position += (header.MetaDataSize - sizeof(PvrMeta));
            }

            var test = (ulong)header.PixelFormat >> 32;

            if (header.NumSurfaces != 1 ||
                header.Depth != 1)
            {
                throw new NotSupportedException();
            }

            var comp = TextureCompressionFormat.Uncompressed;
            TextureFormat format;
            uint blockSize = 0;

            switch (header.PixelFormat)
            {
                case PixelFormat.ETC2_RGB:
                    comp = TextureCompressionFormat.Etc2;
                    if (header.ColorSpace == ColorSpace.sRGB)
                        format = TextureFormat.SRgb24;
                    else
                        format = TextureFormat.Rgb24;
                    break;
                case PixelFormat.ETC2_RGBA:
                    comp = TextureCompressionFormat.Etc2;
                    if (header.ColorSpace == ColorSpace.sRGB)
                        format = TextureFormat.SRgba32;
                    else
                        format = TextureFormat.Rgba32;
                    break;
                case PixelFormat.ETC1:
                    comp = TextureCompressionFormat.Etc1;
                    format = TextureFormat.Rgb24;
                    break;
                case PixelFormat.RGB8:
                    if (header.ColorSpace == ColorSpace.LinearRGB)
                        format = TextureFormat.Rgb24;
                    else
                        format = TextureFormat.SRgb24;
                    break;
                case PixelFormat.RGBFloat32:
                    format = TextureFormat.RgbFloat32;
                    break;
                case PixelFormat.RGBAFloat16:
                    format = TextureFormat.RgbaFloat16;
                    break;
                case PixelFormat.ASTC_4x4:
                case PixelFormat.ASTC_6x6:
                case PixelFormat.ASTC_8x8:
                    if (header.ColorSpace == ColorSpace.sRGB)
                        format = TextureFormat.SRgba32;
                    else
                        format = TextureFormat.Rgba32;
                    comp = TextureCompressionFormat.Astc;
                    blockSize = header.PixelFormat switch
                    {
                        PixelFormat.ASTC_4x4 => 4,
                        PixelFormat.ASTC_6x6 => 6,
                        PixelFormat.ASTC_8x8 => 8,
                        _ => throw new NotSupportedException()
                    };
                    break;
                default:
                    throw new NotSupportedException();
            }

            return ReadData(seekStream, header.Width, header.Height, header.MIPMapCount, header.NumFaces, comp, format, blockSize);

        }

        protected override bool CanHandleExtension(string extension)
        {
            return extension == ".pvr";
        }

        public static readonly PvrTranscoder Instance = new();
    }
}
