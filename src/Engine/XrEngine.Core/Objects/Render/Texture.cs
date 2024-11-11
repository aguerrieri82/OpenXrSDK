namespace XrEngine
{

    public enum TextureFormat
    {
        Unknown,

        Depth32Float,
        Depth24Float,
        Depth24Stencil8,
        Depth32Stencil8,

        Rgb24,
        Rgba32,
        Bgra32,

        Rg88,

        RgbFloat32,
        RgbaFloat32,

        RgbFloat16,
        RgbaFloat16,

        RgFloat32,

        RFloat32,

        SRgb24,
        SBgra32,
        SRgba32,

        Gray8,
        Gray16
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
        Repeat = 10497,
        ClampToBorder = 33069,
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
        Depth,
        External,
        Buffer
    }


    public abstract class Texture : EngineObject, IDisposable
    {
        protected Texture() { }

        protected Texture(IList<TextureData> data)
        {
            LoadData(data);
        }

        public void LoadData(TextureData data)
        {
            LoadData([data]);
        }

        public virtual void LoadData(IList<TextureData> data)
        {
            Data = data;
            Width = data[0].Width;
            Format = data[0].Format;
            Compression = data[0].Compression;
            MagFilter = ScaleFilter.Linear;
            MinFilter = data.Count > 1 ? ScaleFilter.LinearMipmapLinear : ScaleFilter.Linear;
            WrapS = WrapMode.ClampToEdge;

            NotifyChanged(ObjectChangeType.Render);
        }

        protected override void OnChanged(ObjectChange change)
        {
            if (change.IsAny(ObjectChangeType.Property, ObjectChangeType.Render))
                Version++;
            base.OnChanged(change);
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

        public IList<TextureData>? Data { get; set; }

        public uint Width { get; set; }

        public WrapMode WrapS { get; set; }

        public ScaleFilter MagFilter { get; set; }

        public ScaleFilter MinFilter { get; set; }

        public TextureFormat Format { get; set; }

        public TextureCompressionFormat Compression { get; set; }

        public long Handle { get; set; }

        public string? Name { get; set; }
    }
}
