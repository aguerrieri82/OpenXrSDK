using System.Numerics;

namespace OpenXr.WebLink.Entities
{
    public class Mesh
    {
        public Vector3[]? Vertices { get; set; }

        public uint[]? Indices { get; set; }
    }
}
