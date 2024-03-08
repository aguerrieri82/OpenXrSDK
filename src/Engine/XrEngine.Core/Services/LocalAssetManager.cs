
namespace XrEngine
{
    public class LocalAssetManager : IAssetManager
    {
        readonly string _basePath;

        public LocalAssetManager(string basePath)
        {
            _basePath = basePath;
        }

        public Stream OpenAsset(string name)
        {
            return File.OpenRead(FullPath(name));
        }

        public string FullPath(string name)
        {
            return Path.GetFullPath(Path.Combine(_basePath, name));
        }

    }
}
