using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrMath;

namespace XrEngine.Interaction
{
    public interface IRaySource
    {
        Ray3 GetRay();
    }
}
