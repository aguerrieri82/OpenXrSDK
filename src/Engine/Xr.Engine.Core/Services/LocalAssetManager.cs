using OpenXr.Engine.Abstraction;

namespace OpenXr.Engine
{
    public class LocalAssetManager : IAssetManager
    {
        public Stream OpenAsset(string name)
        {
            return File.OpenRead(name);
        }

        public string FullPath(string name)
        {
            return Path.GetFullPath(name);  
        }

        public static readonly LocalAssetManager Instance = new LocalAssetManager();
    }
}
