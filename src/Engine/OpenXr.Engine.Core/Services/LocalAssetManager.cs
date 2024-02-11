using OpenXr.Engine.Abstraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
    public class LocalAssetManager : IAssetManager
    {
        public Stream OpenAsset(string name)
        {
            return File.OpenRead(name);
        }

        public static readonly LocalAssetManager Instance = new LocalAssetManager();
    }
}
