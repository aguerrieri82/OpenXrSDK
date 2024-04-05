using SkiaSharp;

namespace XrEngine
{
    public enum TextureFormat
    {
        Unknown,

        Depth32Float,
        Depth24Float,
        Depth24Stencil8,

        Rgb24,
        Rgba32,
        Bgra32,

        RgbFloat32,
        RgbaFloat32,

        RgbFloat16,
        RgbaFloat16,

        RgFloat32,

        SRgb24,
        SBgra32,
        SRgba32,

        Gray8,
    }

    public enum TextureCompressionFormat
    {
        Uncompressed = 0,
        Etc2 = 0x32435445,
        Etc1 = 0x31435445
    }

    public enum WrapMode
    {
        ClampToEdge = 33071,
        Repeat = 10497
    }

    public enum ScaleFilter
    {
        Nearest = 9728,
        Linear = 9729,
        LinearMipmapLinear = 9987,
    }

    public enum TextureType
    {
        Normal,
        Depth
    }


    public class Texture2D : Texture
    {

        public static Texture2D FromPvrImage(Stream stream)
        {
            var data = PvrTranscoder.Instance.Read(stream);
            return FromData(data);
        }

        public static Texture2D FromImage(string fileName)
        {
            using var stream = File.OpenRead(fileName);
            return FromImage(stream);
        }

        public static Texture2D FromImage(byte[] data)
        {
            return FromImage(SKBitmap.Decode(data));
        }

        public static Texture2D FromImage(Stream stream)
        {
            var result = FromImage(SKBitmap.Decode(stream));
            stream.Dispose();
            return result;
        }

        public static Texture2D FromImage(SKBitmap image)
        {
            var data = new TextureData
            {
                Compression = TextureCompressionFormat.Uncompressed,
                Format = ImageUtils.GetFormat(image.ColorType),
                Data = image.GetPixelSpan().ToArray(),
                Height = (uint)image.Height,
                Width = (uint)image.Width,
            };

            image.Dispose();

            return FromData([data]);
        }

        public static Texture2D FromData(IList<TextureData> data)
        {
            return new Texture2D(data);
        }

        public Texture2D() { }

        public Texture2D(IList<TextureData> data)
            : base(data)
        {
            Height = data[0].Height;
            WrapT = WrapMode.ClampToEdge;

        }

        public uint Height { get; set; }

        public WrapMode WrapT { get; set; }

        public TextureType Type { get; set; }

        public uint SampleCount { get; set; }

        public uint MaxLevels { get; set; }
    }
}
