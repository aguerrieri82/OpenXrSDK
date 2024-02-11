using Android.Content;
using OpenXr.Engine.Abstraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Test.Android
{
    public class AndroidAssetManager : IAssetManager
    {
        Context _context;

        public AndroidAssetManager(Context context)
        {
            _context = context;
        }

        public Stream OpenAsset(string name)
        {
            var test = _context.Assets.List("");

            return _context.Assets!.Open(name);
        }
    }
}
