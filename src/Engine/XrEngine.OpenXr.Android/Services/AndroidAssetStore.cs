using Android.Content.Res.Loader;
using XrEngine.Services;
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

        public string GetPath(string name)
        {
            var cacheBase = Path.Join(_context.CacheDir!.Path, _basePath);

            if (name.StartsWith(cacheBase))
                name = name.Substring(cacheBase.Length + 1);

            var cachePath = Path.Join(cacheBase, name);
            if (_loadedFiles.Contains(cachePath))
                return cachePath;

            /*
            if (File.Exists(cachePath))
                return cachePath;
            */

            Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);

            if (name.StartsWith('/'))
                name = name.Substring(1);

            using var srcStream = _context.Assets!.Open(Path.Join(_basePath, name));
            using var dstStream = File.OpenWrite(cachePath);
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
