using SkiaSharp;

namespace OpenXr.Engine
{
    public enum TextureFormat
    {
        Depth32Float,
        Depth24Float,

        Rgb24,
        Rgba32,
        Bgra32,

        SRgb24,
        SBgra32,
        SRgba32,

        Gray8
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
        static readonly Dictionary<SKColorType, TextureFormat> FORMAT_MAP = new() {
            { SKColorType.Bgra8888, TextureFormat.Bgra32 },
            { SKColorType.Rgba8888, TextureFormat.Rgba32 }
        };

        public static Texture2D FromImage(string fileName)
        {
            using var stream = File.OpenRead(fileName);

            return FromImage(stream);
        }

        public static Texture2D FromDdsImage(Stream stream)
        {
            var data = DdsReader.Instance.Read(stream);
            return FromData(data);
        }

        public static Texture2D FromPkmImage(Stream stream)
        {
            var data = PkmReader.Instance.Read(stream);
            return FromData(data);
        }
        public static Texture2D FromKtxImage(Stream stream)
        {
            var data = KtxReader.Instance.Read(stream);
            return FromData(data);
        }

        public static Texture2D FromPvrImage(Stream stream)
        {
            var data = PvrDecoder.Instance.Read(stream);
            return FromData(data);
        }

        public static Texture2D FromImage(Stream stream)
        {
            var image = SKBitmap.Decode(stream);

            if (!FORMAT_MAP.TryGetValue(image.ColorType, out var format))
            {
                var newBitmap = new SKBitmap(image.Width, image.Height, SKColorType.Rgba8888, SKAlphaType.Opaque);
                image!.CopyTo(newBitmap, SKColorType.Rgba8888);
                image.Dispose();
                image = newBitmap;
                format = TextureFormat.Rgba32;
            }

            var data = new TextureData
            {
                Compression = TextureCompressionFormat.Uncompressed,
                Format = format,
                Data = image.GetPixelSpan().ToArray(),
                Height = (uint)image.Height,
                Width = (uint)image.Width,
            };

            image.Dispose();
            stream.Dispose();

            return FromData([data]);
        }

        public static Texture2D FromData(IList<TextureData> data)
        {
            return new Texture2D
            {
                Data = data,
                Width = data[0].Width,
                Height = data[0].Height,
                Format = data[0].Format,
                Compression = data[0].Compression,
                MagFilter = ScaleFilter.Linear,
                MinFilter = data.Count > 1 ? ScaleFilter.LinearMipmapLinear : ScaleFilter.Linear,
                WrapS = WrapMode.ClampToEdge,
                WrapT = WrapMode.ClampToEdge,
            };
        }

        public IList<TextureData>? Data { get; set; }

        public TextureType Type { get; set; }

        public uint Width { get; set; }

        public uint Height { get; set; }

        public WrapMode WrapS { get; set; }

        public WrapMode WrapT { get; set; }

        public ScaleFilter MagFilter { get; set; }

        public ScaleFilter MinFilter { get; set; }

        public TextureFormat Format { get; set; }

        public TextureCompressionFormat Compression { get; set; }

        public Transform2? Transform { get; set; }

        public uint SampleCount { get; set; }
    }
}
