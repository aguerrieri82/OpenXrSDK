using System.Numerics;

namespace XrMath
{
    public struct Mesh2
    {
        public Mesh2()
        {
            Vertices = [];
            Indices = [];
        }

        public Vector2[] Vertices;

        public uint[] Indices;
    }
}
