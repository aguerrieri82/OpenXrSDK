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
            if (destObj == null && _cache.TryGetValue(uri, out var obj))
                return obj;
            
            obj = GetLoader(uri).LoadAsset(uri, resType, destObj, options);

            if (destObj == null)
                _cache[uri] = obj;

            return obj;
        }

        public void Register(IAssetLoader assetLoader)
        {
            _loaders.Add(assetLoader);
        }


        public static readonly AssetLoader Instance = new();
    }
}
