namespace XrEngine
{
    public class BaseAsset<TOptions, TLoader> : IAsset
        where TOptions : IAssetLoaderOptions
        where TLoader : IAssetLoader
    {
        protected string _name;
        protected Type _type;
        protected Uri _source;
        protected TOptions? _options;
        protected TLoader _loader;

        public BaseAsset(TLoader loader, string name, Type type, Uri source, TOptions? options)
        {
            _loader = loader;
            _name = name;
            _type = type;
            _source = source;
            _options = options;
        }

        public virtual void Delete()
        {
            throw new NotSupportedException();
        }

        public virtual void Rename(string rename)
        {
            throw new NotSupportedException();
        }

        public virtual void Update(EngineObject destObj)
        {
            _loader.LoadAsset(Source, Type, destObj, Options);
        }

        public EngineObject Load()
        {
            var result = _loader.LoadAsset(Source, Type, null, Options);
            result.AddComponent(new AssetSource { Asset = this });
            return result;
        }

        public Type Type => _type;

        public string Name => _name;

        public Uri Source => _source;

        public IAssetLoaderOptions? Options => _options;

    }
}
