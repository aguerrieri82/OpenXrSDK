﻿

namespace XrEngine
{
    public class LocalAssetStore : IAssetStore
    {
        readonly string _basePath;

        public LocalAssetStore(string basePath)
        {
            _basePath = Path.GetFullPath(basePath);
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

        public IEnumerable<string> List(string path)
        {
            return Directory.EnumerateFiles(Path.Join(_basePath, path))
                  .Select(a => a.Substring(_basePath.Length));
        }
    }
}
