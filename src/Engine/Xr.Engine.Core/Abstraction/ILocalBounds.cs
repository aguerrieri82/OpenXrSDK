using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xr.Engine
{
    public interface ILocalBounds
    {
        Bounds3 LocalBounds { get; }
    }
}
