using System.Numerics;
using XrMath;

namespace XrEngine
{
    public readonly struct MeshBuilder
    {
        public MeshBuilder()
        {

        }
        public MeshBuilder AddTriangle(Vector3 a, Vector3 b, Vector3 c)
        {
            return AddTriangle(a, b, c, Vector2.Zero, Vector2.Zero, Vector2.Zero);
        }


        public MeshBuilder AddTriangle(Vector3 a, Vector3 b, Vector3 c, Vector2 uvA, Vector2 uvB, Vector2 uvC)
        {
            var triangle = new Triangle3 { V0 = a, V1 = b, V2 = c };
            var normal = triangle.Normal();

            Vertices.Add(new VertexData { Pos = a, Normal = normal, UV = uvA });
            Vertices.Add(new VertexData { Pos = b, Normal = normal, UV = uvB });
            Vertices.Add(new VertexData { Pos = c, Normal = normal, UV = uvC });

            return this;
        }

        public MeshBuilder AddFace(Vector3 a, Vector3 b, Vector3 c, Vector3 d, bool reverse = false)
        {
            return AddFace(a, b, c, d, Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero, reverse);
        }

        public MeshBuilder AddFace(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 uvA, Vector2 uvB, Vector2 uvC, Vector2 uvD, bool reverse = false)
        {
            if (reverse)
            {
                AddTriangle(d, b, a, uvD, uvB, uvA);
                AddTriangle(d, c, b, uvD, uvC, uvB);
            }
            else
            {
                AddTriangle(a, b, d, uvA, uvB, uvD);
                AddTriangle(b, c, d, uvB, uvC, uvD);
            }
            return this;
        }

        public MeshBuilder AddCircle(Vector3 center, float radius, float subs)
        {
            for (var i = 0; i < subs; i++)
            {
                var a1 = MathF.PI * 2 * i / subs;
                var a2 = MathF.PI * 2 * (i + 1) / subs;
                var v1 = center + new Vector3(MathF.Cos(a1) * radius, MathF.Sin(a1) * radius, 0);
                var v2 = center + new Vector3(MathF.Cos(a2) * radius, MathF.Sin(a2) * radius, 0);
                AddTriangle(v1, center, v2);
            }
            return this;
        }

        public MeshBuilder AddCylinder(Vector3 center, float radius, float height, float subs)
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
            return this;
        }

        public MeshBuilder AddCone(Vector3 center, float radius, float height, float subs)
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
            return this;
        }

        public MeshBuilder AddRevolve(ICurve2D curve, float subs, float startAngle = 0f, float endAngle = MathF.PI * 2)
        {
            var step = (endAngle - startAngle) / subs;

            var samples = curve
                .Sample(0.001f, 1000)
                .ToArray();

            for (var i = 0; i < subs; i++)
            {
                var a1 = startAngle + step * i;
                var a2 = startAngle + step * (i + 1);

                var q1 = Quaternion.CreateFromAxisAngle(Vector3.UnitY, a1);
                var q2 = Quaternion.CreateFromAxisAngle(Vector3.UnitY, a2);

                var v1 = a1 / (MathF.PI * 2);
                var v2 = a2 / (MathF.PI * 2);

                for (var j = 0; j < samples.Length - 1; j++)
                {
                    var s1 = samples[j].Position.ToVector3();
                    var s2 = samples[j + 1].Position.ToVector3();

                    var vr0 = Vector3.Transform(s1, q1);
                    var vr1 = Vector3.Transform(s2, q1);
                    var vr2 = Vector3.Transform(s1, q2);
                    var vr3 = Vector3.Transform(s2, q2);

                    var u1 = samples[j].Time;
                    var u2 = samples[j + 1].Time;   

                    var uv0 = new Vector2(u1, v1);
                    var uv1 = new Vector2(u2, v1);
                    var uv2 = new Vector2(u1, v2);
                    var uv3 = new Vector2(u2, v2);

                    AddFace(vr0, vr1, vr3, vr2, uv0, uv1, uv3, uv2, true);
                }
            }

            return this;
        }

        public Geometry3D ToGeometry()
        {
            var geo = new Geometry3D();
            geo.Vertices = Vertices.ToArray();
            geo.ActiveComponents |= VertexComponent.UV0;
            geo.ComputeIndices();
            geo.ComputeNormals();
            //geo.SmoothNormals();

            return geo;
        }

        public readonly IList<VertexData> Vertices = [];
    }
}
