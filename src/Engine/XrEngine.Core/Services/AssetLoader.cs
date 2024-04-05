namespace XrEngine.Services
{
    public class AssetLoader
    {
        readonly List<IAssetHandler> _loaders = [];


        AssetLoader()
        {
            Register(DdsReader.Instance);
            Register(ExrReader.Instance);
            Register(HdrReader.Instance);
            Register(ImageReader.Instance);
            Register(Ktx2Reader.Instance);
            Register(KtxReader.Instance);
            Register(PkmReader.Instance);
            Register(PvrTranscoder.Instance);
        }

        public EngineObject Load(Uri uri, Type resType, object? options = null)
        {
            var loader = _loaders.FirstOrDefault(a => a.CanHandle(uri, out resType));
            if (loader == null)
                throw new NotSupportedException();

            return loader.LoadAsset(uri, resType, AssetManager!, options);
        }

        public void Register(IAssetHandler assetLoader)
        {
            _loaders.Add(assetLoader);
        }

        public IAssetManager? AssetManager { get; set; }


        public static readonly AssetLoader Instance = new();
    }
}
