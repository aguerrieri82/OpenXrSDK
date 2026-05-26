using System.Numerics;

namespace XrMath
{
    public struct Mesh3
    {
        public Mesh3()
        {
            Vertices = [];
            Indices = [];
        }

        public Vector3[] Vertices;

        public uint[] Indices;
    }
}
