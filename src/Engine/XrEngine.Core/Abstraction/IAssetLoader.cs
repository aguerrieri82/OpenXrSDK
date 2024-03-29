using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine
{
    public interface IAssetLoader
    {
        bool CanHandle(Uri uri, out Type resType);

        object LoadAsset(Uri uri, Type resType, IAssetManager assetManager, object? options = null);


    }
}
