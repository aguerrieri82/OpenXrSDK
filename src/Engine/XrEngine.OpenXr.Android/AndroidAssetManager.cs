﻿using Android.Content;

namespace XrEngine.OpenXr.Android
{
    public class AndroidAssetManager : IAssetManager
    {
        readonly Context _context;
        readonly string _basePath;

        public AndroidAssetManager(Context context, string basePath)
        {
            _context = context;
            _basePath = basePath;
        }

        public string GetFsPath(string name)
        {
            var cacheBase = Path.Join(_context.CacheDir!.Path, _basePath);

            if (name.StartsWith(cacheBase))
                name = name.Substring(cacheBase.Length + 1);

            var cachePath = Path.Join(cacheBase, name);
            if (File.Exists(cachePath))
                return cachePath;

            Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);

            using var srcStream = _context.Assets!.Open(Path.Join(_basePath, name));
            using var dstStream = File.OpenWrite(cachePath);
            srcStream.CopyTo(dstStream);
            return cachePath;
        }

        public IEnumerable<string> List(string path)
        {
            return _context.Assets!.List(Path.Join(_basePath, path))!
                .Select(a => Path.Join(path, a));
        }

        public Stream Open(string name)
        {
            return File.OpenRead(GetFsPath(name));
        }
    }
}