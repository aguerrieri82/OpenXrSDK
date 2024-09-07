namespace XrEngine
{
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

            Version++;
        }

        public void NotifyLoaded()
        {
            Data = null;
        }

        public IList<TextureData>? Data { get; set; }

        public uint Width { get; set; }

        public WrapMode WrapS { get; set; }

        public ScaleFilter MagFilter { get; set; }

        public ScaleFilter MinFilter { get; set; }

        public TextureFormat Format { get; set; }

        public TextureCompressionFormat Compression { get; set; }

        public string? Name { get; set; }
    }
}
