using System;
using System.Collections.Generic;
using System.Text;
using XrEngine;

namespace XrSamples.Graffiti
{
    public class SprayBrush : TriangleMesh
    {

        public SprayBrush(int radialSubs, int innerSubs)
        {
            var builder = new MeshBuilder();

            Geometry = builder.ToGeometry();
        }
    }
}
