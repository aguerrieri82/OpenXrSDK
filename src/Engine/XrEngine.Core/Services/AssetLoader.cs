namespace XrEngine.Services
{
    public class AssetLoader
    {
        readonly List<IAssetLoader> _loaders = [];

        AssetLoader()
        {
        }

        public IAssetLoader GetLoader(Uri uri)
        {
            var loader = _loaders.FirstOrDefault(a => a.CanHandle(uri, out var resType));
            if (loader == null)
                throw new NotSupportedException();
            return loader;
        }

        public EngineObject Load(Uri uri, Type resType, EngineObject? destObj, IAssetLoaderOptions? options = null)
        {
            return GetLoader(uri).LoadAsset(uri, resType, destObj, options);
        }

        public void Register(IAssetLoader assetLoader)
        {
            _loaders.Add(assetLoader);
        }


        public static readonly AssetLoader Instance = new();
    }
}
