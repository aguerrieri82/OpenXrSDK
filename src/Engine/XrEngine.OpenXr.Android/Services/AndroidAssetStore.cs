using Context2 = global::Android.Content.Context;

namespace XrEngine.OpenXr.Android
{
    public class AndroidAssetStore : IAssetStore
    {
        readonly Context2 _context;
        readonly string _basePath;
        readonly HashSet<string> _loadedFiles = [];

        public AndroidAssetStore(Context2 context, string basePath)
        {
            _context = context;
            _basePath = basePath;
        }

        public bool Contains(string name)
        {
            string? path = Path.GetDirectoryName(name);
            string file = Path.GetFileName(name);
            string fullPath = Path.Join(_basePath, path);
            if (fullPath.Length > 0 && fullPath[0] == '/')
                fullPath = fullPath.Substring(1);
            string[]? result = _context.Assets!.List(fullPath);
            if (result == null)
                return false;
            return result.Contains(file);
        }

        public string GetPath(string name)
        {
            string cacheBase = Path.Join(_context.CacheDir!.Path, _basePath);

            if (name.StartsWith(cacheBase))
                name = name.Substring(cacheBase.Length + 1);

            string cachePath = Path.Join(cacheBase, name);
            if (_loadedFiles.Contains(cachePath))
                return cachePath;

            if (File.Exists(cachePath))
                return cachePath;

            Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);

            if (name.StartsWith('/'))
                name = name.Substring(1);

            using Stream srcStream = _context.Assets!.Open(Path.Join(_basePath, name));
            using FileStream dstStream = File.OpenWrite(cachePath);
            srcStream.CopyTo(dstStream);

            _loadedFiles.Add(cachePath);

            return cachePath;
        }

        public IEnumerable<string> List(string path)
        {

            return _context.Assets!.List(Path.Join(_basePath, path))!
                .Select(a => Path.Join(path, a));
        }

        public IEnumerable<string> ListDirectories(string storePath)
        {
            throw new NotSupportedException();
        }

        public Stream Open(string name)
        {
            return File.OpenRead(GetPath(name));
        }
    }
}
