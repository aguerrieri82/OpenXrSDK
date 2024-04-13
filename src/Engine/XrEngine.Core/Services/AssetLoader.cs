﻿namespace XrEngine.Services
{
    public class AssetLoader
    {
        readonly List<IAssetHandler> _loaders = [];

        AssetLoader()
        {

        }

        public EngineObject Load(Uri uri, Type resType, EngineObject? destObj, IAssetLoaderOptions? options = null)
        {
            var loader = _loaders.FirstOrDefault(a => a.CanHandle(uri, out resType));
            if (loader == null)
                throw new NotSupportedException();

            return loader.LoadAsset(uri, resType, AssetManager!, destObj, options);
        }

        public void Register(IAssetHandler assetLoader)
        {
            _loaders.Add(assetLoader);
        }

        public IAssetManager? AssetManager { get; set; }


        public static readonly AssetLoader Instance = new();
    }
}
