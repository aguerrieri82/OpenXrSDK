using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrMath;

namespace XrEngine
{
    public interface IOutlineSource
    {
        bool HasOutlines();

        bool HasOutline(Object3D obj, out Color color);
    }
}
