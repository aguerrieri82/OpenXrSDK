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

        public Stream OpenAsset(string name)
        {
            return _context.Assets!.Open(name);
        }
    }
}
