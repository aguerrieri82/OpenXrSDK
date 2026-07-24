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

        public Texture2D()
        {
        }

        public Texture2D(IList<TextureData> data)
            : base(data)
        {
        }

        public override void LoadData(IList<TextureData> data, bool initSampler = true)
        {
            if (data.Count == 0)
                throw new InvalidOperationException("Texture data is empty");

            Height = data.Max(a => a.Height);

            var depthFromLayers = data.Max(a => a.Layer) + 1;
            var depthFromData = data.Max(a => a.Depth);

            Depth = Math.Max(depthFromLayers, Math.Max(depthFromData, 1));

            if (data.Count > 1)
                MipLevelCount = data.Max(a => a.MipLevel) + 1;
            else
                MipLevelCount = 0;

            base.LoadData(data, initSampler);
        }

        public void SetDescription(
            uint width,
            uint height,
            TextureFormat format,
            TextureCompressionFormat compression = TextureCompressionFormat.Uncompressed,
            bool initSampler = true)
        {
            SetDescription(width, height, 1, format, compression, 0, initSampler);
        }

        public void SetDescription(
            uint width,
            uint height,
            uint depth,
            TextureFormat format,
            TextureCompressionFormat compression = TextureCompressionFormat.Uncompressed,
            uint mipLevelCount = 0,
            bool initSampler = true)
        {
            Height = height;
            Depth = Math.Max(depth, 1);
            MipLevelCount = mipLevelCount;

            base.SetDescription(width, format, compression, initSampler);
        }

        protected override void InitSampler()
        {
            if (WrapT == 0)
                WrapT = WrapMode.ClampToEdge;

            if (MipLevelCount > 0 && MinFilter == 0)
                MinFilter = ScaleFilter.LinearMipmapLinear;

            base.InitSampler();
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

        public Task? UpdateTask { get; set; }

        public static readonly Texture2D DepthBuffer = new() { Type = TextureType.Depth };
    }
}