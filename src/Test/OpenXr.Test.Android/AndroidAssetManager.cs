using Android.Content;
using OpenXr.Engine.Abstraction;

namespace OpenXr.Test.Android
{
    public class AndroidAssetManager : IAssetManager
    {
        readonly Context _context;

        public AndroidAssetManager(Context context)
        {
            _context = context;
        }

        public string FullPath(string name)
        {

            string mainDir =Path.Combine(_context.FilesDir!.Path, name);
            if (File.Exists(mainDir))
                return mainDir;

            Directory.CreateDirectory(Path.GetDirectoryName(mainDir)!); 

            using var srcStream = _context.Assets!.Open(name);
            using var dstStream = File.OpenWrite(mainDir);
            srcStream.CopyTo(dstStream);
            return mainDir;
        }

        public Stream OpenAsset(string name)
        {
            var fullPath = _context.FilesDir!.Path;
            if (name.StartsWith(fullPath))
                name = name.Substring(fullPath.Length + 1);

            return _context.Assets!.Open(name);
        }
    }
}
