using Android.Content;
using Java.Nio.FileNio;
using OpenXr.Engine.Abstraction;
using static Java.Util.Jar.Attributes;

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

            string mainDir = System.IO.Path.Combine( _context.FilesDir!.Path, name);
            if (File.Exists(mainDir))
                return mainDir;
            using var srcStream = _context.Assets!.Open(name);
            using var dstStream = System.IO.File.OpenWrite(mainDir);
            srcStream.CopyTo(dstStream);
            return mainDir;
        }

        public Stream OpenAsset(string name)
        {
            return _context.Assets!.Open(name);
        }
    }
}
