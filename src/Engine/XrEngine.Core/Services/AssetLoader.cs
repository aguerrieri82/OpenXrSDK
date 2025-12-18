namespace XrEngine
{
    public class AssetLoader
    {
        readonly List<IAssetLoader> _loaders = [];
        readonly Dictionary<Uri, EngineObject> _cache = [];

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
            var useCache = destObj == null && (options == null || options.UseCache);


            EngineObject obj;

            lock (_cache)
            {
                if (useCache && _cache.TryGetValue(uri, out obj!))
                    return obj;

            }

            obj = GetLoader(uri).LoadAsset(uri, resType, destObj, options);

            lock (_cache)
            {
                if (destObj == null && useCache)
                    _cache[uri] = obj;
            }

            return obj;
        }

        public void Register(IAssetLoader assetLoader)
        {
            _loaders.Add(assetLoader);
        }


        public static readonly AssetLoader Instance = new();
    }
}
