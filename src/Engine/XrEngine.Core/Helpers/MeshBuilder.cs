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

            AddVertices(new VertexData { Pos = a, Normal = normal, UV = uvA },
                        new VertexData { Pos = b, Normal = normal, UV = uvB },
                        new VertexData { Pos = c, Normal = normal, UV = uvC });

            return this;
        }

        public MeshBuilder AddVertices(params VertexData[] vertices)
        {
            Vertices.AddRange(vertices);
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

            Colliders.Add(new CapsuleCollider
            {
                Radius = radius,
                Height = height,
                Pose = new Pose3
                {
                    Position = center + new Vector3(0, 0, height / 2),
                    Orientation = Quaternion.Identity
                }
            });

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

        public MeshBuilder AddSphere(Vector3 center, float radius, int subs)
        {
            var vertices = new List<VertexData>();

            for (int lat = 0; lat <= subs; lat++)
            {
                float theta = lat * MathF.PI / subs;
                float sinTheta = MathF.Sin(theta);
                float cosTheta = MathF.Cos(theta);

                for (int lon = 0; lon <= subs; lon++)
                {
                    float phi = lon * 2 * MathF.PI / subs;
                    float sinPhi = MathF.Sin(phi);
                    float cosPhi = MathF.Cos(phi);

                    float x = sinTheta * cosPhi;
                    float y = cosTheta;
                    float z = sinTheta * sinPhi;

                    var tangent = new Vector3(-sinPhi, 0, cosPhi).Normalize();

                    vertices.Add(new VertexData
                    {
                        Pos = center + new Vector3(x, y, z) * radius,
                        Normal = new Vector3(x, y, z).Normalize(),
                        Tangent = new Vector4(tangent, 1),
                        UV = new Vector2((float)lon / subs, (float)lat / subs)
                    });
                }
            }

            var indices = new List<uint>();

            for (int lat = 0; lat < subs; lat++)
            {
                for (int lon = 0; lon < subs; lon++)
                {
                    int first = lat * (subs + 1) + lon;
                    int second = first + subs + 1;

                    AddVertices(
                        vertices[first + 1],
                        vertices[second],
                        vertices[first],
                        vertices[first + 1],
                        vertices[second + 1],
                        vertices[second]);

                }
            }

            Colliders.Add(new SphereCollider
            {
                Center = center,
                Radius = radius,
            });

            return this;
        }

        public MeshBuilder AddCube(Vector3 center, Vector3 size)
        {
            var halfSize = size / 2;

            var vertices = VertexData.FromPosNormalUV(
            [
                //X    Y      Z       Normals UV
                halfSize.X, halfSize.Y, -halfSize.Z, -0f, 1f, -0f, 1f, 1f,
                -halfSize.X, halfSize.Y, -halfSize.Z, -0f, 1f, -0f, 0f, 1f,
                -halfSize.X, halfSize.Y, halfSize.Z, -0f, 1f, -0f, 0f, 0f,
                halfSize.X, halfSize.Y, halfSize.Z, -0f, 1f, -0f, 1f, 0f,
                halfSize.X, -halfSize.Y, halfSize.Z, -0f, -0f, 1f, 1f, 0f,
                halfSize.X, halfSize.Y, halfSize.Z, -0f, -0f, 1f, 1f, 1f,
                -halfSize.X, halfSize.Y, halfSize.Z, -0f, -0f, 1f, 0f, 1f,
                -halfSize.X, -halfSize.Y, halfSize.Z, -0f, -0f, 1f, 0f, 0f,
                -halfSize.X, -halfSize.Y, halfSize.Z, -1f, -0f, -0f, 0f, 0f,
                -halfSize.X, halfSize.Y, halfSize.Z, -1f, -0f, -0f, 0f, 1f,
                -halfSize.X, halfSize.Y, -halfSize.Z, -1f, -0f, -0f, 1f, 1f,
                -halfSize.X, -halfSize.Y, -halfSize.Z, -1f, -0f, -0f, 1f, 0f,
                -halfSize.X, -halfSize.Y, -halfSize.Z, -0f, -1f, -0f, 0f, 1f,
                halfSize.X, -halfSize.Y, -halfSize.Z, -0f, -1f, -0f, 1f, 1f,
                halfSize.X, -halfSize.Y, halfSize.Z, -0f, -1f, -0f, 1f, 0f,
                -halfSize.X, -halfSize.Y, halfSize.Z, -0f, -1f, -0f, 0f, 0f,
                halfSize.X, -halfSize.Y, -halfSize.Z, 1f, -0f, -0f, 1f, 0f,
                halfSize.X, halfSize.Y, -halfSize.Z, 1f, -0f, -0f, 1f, 1f,
                halfSize.X, halfSize.Y, halfSize.Z, 1f, -0f, -0f, 0f, 1f,
                halfSize.X, -halfSize.Y, halfSize.Z, 1f, -0f, -0f, 0f, 0f,
                -halfSize.X, -halfSize.Y, -halfSize.Z, -0f, -0f, -1f, 0f, 0f,
                -halfSize.X, halfSize.Y, -halfSize.Z, -0f, -0f, -1f, 0f, 1f,
                halfSize.X, halfSize.Y, -halfSize.Z, -0f, -0f, -1f, 1f, 1f,
                halfSize.X, -halfSize.Y, -halfSize.Z, -0f, -0f, -1f, 1f, 0f,
             ]);

            uint[] indices =
            [
                 0,1,2,
                 0,2,3,
                 4,5,6,
                 4,6,7,
                 8,9,10,
                 8,10,11,
                 12,13,14,
                 12,14,15,
                 16,17,18,
                 16,18,19,
                 20,21,22,
                 20,22,23,
             ];

            foreach (var idx in indices)
            {
                var vrt = vertices[idx];
                vrt.Pos += center;
                Vertices.Add(vrt);
            }

            Colliders.Add(new BoxCollider
            {
                Center = center,
                Size = size,
            });

            return this;
        }


        public Geometry3D ToGeometry()
        {
            var geo = new Geometry3D();
            geo.Vertices = Vertices.ToArray();
            geo.ActiveComponents |= VertexComponent.UV0;
            geo.ComputeIndices();
            geo.ComputeNormals();
            return geo;
        }

        public void AddColliders(Object3D obj)
        {
            foreach (var collider in Colliders)
                obj.AddComponent((IComponent)collider);
        }

        public readonly List<ICollider3D> Colliders = [];

        public readonly List<VertexData> Vertices = [];
    }
}
