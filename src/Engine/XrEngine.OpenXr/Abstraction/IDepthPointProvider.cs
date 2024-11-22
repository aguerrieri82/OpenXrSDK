using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine.OpenXr
{
    public interface IDepthPointProvider
    {
        Vector3[]? ReadPoints(IEnvDepthProvider provider);
    }
}
