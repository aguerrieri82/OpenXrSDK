using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine.Services
{
    public class AssetLoader
    {
        readonly List<IAssetLoader> _loaders = [];


        AssetLoader()
        {

        }

        public EngineObject Load(Uri uri, Type resType, object? options = null)
        {
            
            var loader = _loaders.FirstOrDefault(a => a.CanHandle(uri, out resType));
            if (loader == null)
                throw new NotSupportedException();

            return loader.LoadAsset(uri, resType, AssetManager!, options);   
        }

        public void Register(IAssetLoader assetLoader)
        {
            _loaders.Add(assetLoader);  
        }

        public IAssetManager? AssetManager { get; set; }


        public static readonly AssetLoader Instance = new();
    }
}
