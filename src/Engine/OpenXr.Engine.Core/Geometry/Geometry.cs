using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{

    public abstract class Geometry : EngineObject
    {


        public int[]? Indices { get; set; }

        public VertexData[]? Vertices {  get; set; }    
    }
}
