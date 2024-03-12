using System.Numerics;

namespace XrMath
{
    public struct Mesh
    {
        public Mesh()
        {
            Vertices = [];
            Indices = [];
        }

        public Vector3[] Vertices;

        public uint[] Indices;
    }
}
