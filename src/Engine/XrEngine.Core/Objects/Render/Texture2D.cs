using Common.Interop;
using SkiaSharp;
using XrMath;

namespace XrEngine
{

    public class Texture2D : Texture
    {
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
                Data = MemoryBuffer.Create(image.Bytes),
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
        }

        public override void LoadData(IList<TextureData> data, bool initSampler = true)
        {
            Height = data[0].Height;

            if (initSampler)
            {
                WrapT = WrapMode.ClampToEdge;
                MipLevelCount = data.Max(a => a.MipLevel) + 1;
            }
            base.LoadData(data, initSampler);
        }

        public uint Height { get; set; }

        public float MaxAnisotropy { get; set; }

        public WrapMode WrapT { get; set; }

        public TextureType Type { get; set; }

        public uint SampleCount { get; set; }

        public uint MipLevelCount { get; set; }

        public Matrix3x3? Transform { get; set; }

        public Color BorderColor { get; set; }

        public uint Depth { get; set; }


        public static readonly Texture2D DepthBuffer = new() { Type = TextureType.Depth };
    }
}
