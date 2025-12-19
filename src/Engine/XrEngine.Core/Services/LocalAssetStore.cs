namespace XrEngine
{
    public class LocalAssetStore : IAssetStore
    {
        readonly string _basePath;

        public LocalAssetStore(string basePath)
        {
            _basePath = Path.GetFullPath(basePath);
        }


        public bool Contains(string name)
        {
            return File.Exists(GetPath(name));
        }

        public Stream Open(string name)
        {
            return File.OpenRead(GetPath(name));
        }

        public string GetPath(string name)
        {
            if (name.StartsWith(_basePath, StringComparison.OrdinalIgnoreCase))
                return name;
            return Path.Join(_basePath, name);
        }

        public IEnumerable<string> List(string storePath)
        {
            var path = Path.Join(_basePath, storePath);
            if (!Directory.Exists(path))
                return [];
            return Directory.EnumerateFiles(path)
                  .Select(a => a.Substring(_basePath.Length));
        }

        public IEnumerable<string> ListDirectories(string storePath)
        {
            var path = Path.Join(_basePath, storePath);
            if (!Directory.Exists(path))
                return [];
            return Directory.EnumerateDirectories(path)
                  .Select(a => a.Substring(_basePath.Length));
        }

    }
}
