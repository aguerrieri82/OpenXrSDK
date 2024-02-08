using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.WebLink.Entities
{
    public class Mesh
    {
        public Vector3f[]? Vertices { get; set; }

        public uint[]? Indices { get; set; }
    }
}
