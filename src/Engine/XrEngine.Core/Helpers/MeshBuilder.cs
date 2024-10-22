using System.Numerics;
using XrMath;

namespace XrEngine
{
    public readonly struct MeshBuilder
    {
        public MeshBuilder()
        {

        }

        public void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
        {
            var triangle = new Triangle3 { V0 = a, V1 = b, V2 = c };
            var normal = triangle.Normal();

            Vertices.Add(new VertexData { Pos = a, Normal = normal });
            Vertices.Add(new VertexData { Pos = b, Normal = normal });
            Vertices.Add(new VertexData { Pos = c, Normal = normal });
        }

        public void AddFace(Vector3 a, Vector3 b, Vector3 c, Vector3 d, bool reverse = false)
        {
            if (reverse)
            {
                AddTriangle(d, b, a);
                AddTriangle(d, c, b);
            }
            else
            {
                AddTriangle(a, b, d);
                AddTriangle(b, c, d);
            }

        }

        public void AddCircle(Vector3 center, float radius, float subs)
        {
            for (var i = 0; i < subs; i++)
            {
                var a1 = MathF.PI * 2 * i / subs;
                var a2 = MathF.PI * 2 * (i + 1) / subs;
                var v1 = center + new Vector3(MathF.Cos(a1) * radius, MathF.Sin(a1) * radius, 0);
                var v2 = center + new Vector3(MathF.Cos(a2) * radius, MathF.Sin(a2) * radius, 0);
                AddTriangle(v1, center, v2);
            }
        }

        public void AddCylinder(Vector3 center, float radius, float height, float subs)
        {
            for (var i = 0; i < subs; i++)
            {
                var a1 = MathF.PI * 2 * i / subs;
                var a2 = MathF.PI * 2 * (i + 1) / subs;

                var v1 = center + new Vector3(MathF.Cos(a1) * radius, MathF.Sin(a1) * radius, 0);
                var v2 = center + new Vector3(MathF.Cos(a2) * radius, MathF.Sin(a2) * radius, 0);

                var v3 = new Vector3(v1.X, v1.Y, v1.Z + height);
                var v4 = new Vector3(v2.X, v2.Y, v2.Z + height);

                AddFace(v1, v2, v4, v3);
            }
        }

        public void AddCone(Vector3 center, float radius, float height, float subs)
        {
            var top = center + new Vector3(0, 0, height);
            for (var i = 0; i < subs; i++)
            {
                var a1 = MathF.PI * 2 * i / subs;
                var a2 = MathF.PI * 2 * (i + 1) / subs;

                var v1 = center + new Vector3(MathF.Cos(a1) * radius, MathF.Sin(a1) * radius, 0);
                var v2 = center + new Vector3(MathF.Cos(a2) * radius, MathF.Sin(a2) * radius, 0);
                AddTriangle(v2, top, v1);
            }
        }

        public readonly IList<VertexData> Vertices = [];
    }
}
