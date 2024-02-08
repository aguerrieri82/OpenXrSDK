using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
    public abstract class Geometry : EngineObject
    {


        public int[]? Triangles { get; set; }

        public Vector3[]? Vertices {  get; set; }    
    }
}
