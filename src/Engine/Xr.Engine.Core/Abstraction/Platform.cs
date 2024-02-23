using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
    public class Platform
    {
        public IAssetManager? AssetManager { get; set; } 

        public static Platform? Current { get; set; }
    }
}
