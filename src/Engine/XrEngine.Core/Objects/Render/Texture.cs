namespace XrEngine
{

    public enum TextureFormat
    {
        Unknown,

        Depth32Float,
        Depth24,
        Depth24Stencil8,
        Depth32Stencil8,
        Depth16,

        Rgb24,
        Rgba32,
        Bgra32,

        RgbaInt16,
        SRgbaInt16,

        Rg88,

        RgbFloat32,
        RgbaFloat32,

        RgbFloat16,
        RgbaFloat16,
        Rgb9e5Float,

        RgFloat32,
        RgFloat16,

        GrayFloat32,
        GrayFloat16,

        SRgb24,
        SBgra32,
        SRgba32,

        GrayInt8,
        GrayInt16,

        GrayRawSInt16,
        GrayUint32,
        RgUint32
    }

    public enum TextureCompressionFormat
    {
        Uncompressed = 0,
        Etc2 = 0x32435445,
        Etc1 = 0x31435445,
        Bc3 = 0x35545844,
        Bc1 = 0x31545844,
        Bc7 = 0x20374342,
        Astc = 0x43545341
    }

    public enum WrapMode
    {
        ClampToEdge = 33071,
        Repeat = 10497,
        ClampToBorder = 33069,
        MirrorRepeat = 33648,
    }

    public enum ScaleFilter
    {
        Nearest = 9728,
        Linear = 9729,
        LinearMipmapLinear = 9987,
        TriLinear = LinearMipmapLinear
    }

    public enum TextureType
    {
        Unspecified,
        Depth,
        External,
        Buffer,
        NormalMap
    }

    public abstract class Texture : EngineObject, IDisposable, IGpuObject
    {
        protected Texture()
        {
        }

        protected Texture(IList<TextureData> data)
        {
            LoadData(data);
        }

        public void LoadData(TextureData data, bool initSampler = true)
        {
            if (data.Data == null)
                Log.Warn(this, "Tetxure LoadData without data");

            LoadData([data], initSampler);
        }

        public virtual void LoadData(IList<TextureData> data, bool initSampler = true)
        {
            if (data.Count == 0)
                throw new InvalidOperationException("Texture data is empty");

            Data = data;
            Width = data.Max(a => a.Width);

            if (Format == TextureFormat.Unknown)
                Format = data[0].Format;

            Compression = data[0].Compression;

            if (initSampler)
                InitSampler();

            NotifyChanged();
        }

        public virtual void SetDescription(
            uint width,
            TextureFormat format,
            TextureCompressionFormat compression = TextureCompressionFormat.Uncompressed,
            bool initSampler = true)
        {
            Width = width;
            Format = format;
            Compression = compression;
            Data = null;

            if (initSampler)
                InitSampler();

            NotifyChanged();
        }

        protected virtual void InitSampler()
        {
            if (MinFilter == 0)
                MinFilter = ScaleFilter.Linear;

            if (MagFilter == 0)
                MagFilter = ScaleFilter.Linear;

            if (WrapS == 0)
                WrapS = WrapMode.ClampToEdge;
        }

        public void NotifyLoaded()
        {
            Data = null;
        }

        public override void Dispose()
        {
            Data = null;
            Handle = 0;
            base.Dispose();
        }

        public override void GeneratePath(List<string> parts)
        {
            parts.Add($"Texture-{DateTime.UtcNow.Ticks}");
        }

        public void NotifyChanged()
        {
            NotifyChanged(ChangeType.Render);
        }

        public IList<TextureData>? Data { get; set; }

        public uint Width { get; set; }

        public WrapMode WrapS { get; set; }

        public ScaleFilter MagFilter { get; set; }

        public ScaleFilter MinFilter { get; set; }

        public TextureFormat Format { get; set; }

        public TextureCompressionFormat Compression { get; set; }

        public TextureSampler? Sampler { get; set; }

        public bool NeverCompress { get; set; }

        public long Handle { get; set; }

        public string? Name { get; set; }

        public string? Hash { get; set; }
    }

}
