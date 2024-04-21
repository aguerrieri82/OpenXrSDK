using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine
{
    public class EngineAssetLoader : IAssetLoader
    {
        public bool CanHandle(Uri uri, out Type resType)
        {
            throw new NotImplementedException();
        }

        public EngineObject LoadAsset(Uri uri, Type resType, EngineObject? curObj, IAssetLoaderOptions? options = null)
        {
            throw new NotImplementedException();
        }
    }
}
