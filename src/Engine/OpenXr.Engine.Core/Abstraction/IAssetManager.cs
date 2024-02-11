using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine.Abstraction
{
    public interface IAssetManager
    {
        Stream OpenAsset(string name);
    }
}
