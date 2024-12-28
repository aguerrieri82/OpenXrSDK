using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine
{
    public enum TessellationMode
    {
        None,
        Normal,
        Geometry
    }

    public interface ITessellationMaterial : IMaterial
    {

        TessellationMode TessellationMode { get; }

        bool DebugTessellation { get; }
    }
}
