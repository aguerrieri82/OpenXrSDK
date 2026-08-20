using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine
{
    public interface IMorphedMesh
    {
        float[] Weights { get; set; }

        long MorphVersion { get; }
    }
}
