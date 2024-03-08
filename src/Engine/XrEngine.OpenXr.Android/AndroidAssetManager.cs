using Android.Content;
using XrEngine;

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

        public string FullPath(string name)
        {
            var filesBase = Path.Join(_context.FilesDir!.Path, _basePath);

            string mainDir = Path.Join(filesBase, name);
            if (File.Exists(mainDir))
                return mainDir;

            Directory.CreateDirectory(Path.GetDirectoryName(mainDir)!);

            if (name.StartsWith(filesBase))
                name = name.Substring(filesBase.Length + 1);

            using var srcStream = _context.Assets!.Open(Path.Join(_basePath, name));
            using var dstStream = File.OpenWrite(mainDir);
            srcStream.CopyTo(dstStream);
            return mainDir;
        }

        public Stream OpenAsset(string name)
        {
            var fullPath = Path.Join(_context.FilesDir!.Path, _basePath);
            if (name.StartsWith(fullPath))
                name = name.Substring(fullPath.Length + 1);

            return _context.Assets!.Open(Path.Join(_basePath, name));
        }
    }
}
