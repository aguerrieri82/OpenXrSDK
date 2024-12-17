using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using XrMath;

namespace XrEngine
{
    public enum CylinderPart
    {
        TopCap = 0x1,
        BottomCap = 0x2,
        Body = 0x4,
        All = TopCap | BottomCap | Body
    }

    public enum UVMode
    {
        Normalized,
        Size
    }

    public readonly struct MeshBuilder
    {
        public MeshBuilder()
        {
        }
        public MeshBuilder AddTriangle(Triangle3 triangle)
        {
            return AddTriangle(triangle.V0, triangle.V1, triangle.V2, Vector2.Zero, Vector2.Zero, Vector2.Zero);
        }

        public MeshBuilder AddTriangle(Vector3 a, Vector3 b, Vector3 c)
        {
            return AddTriangle(a, b, c, Vector2.Zero, Vector2.Zero, Vector2.Zero);
        }

        public MeshBuilder AddTriangle(Vector3 a, Vector3 b, Vector3 c, Vector2 uvA, Vector2 uvB, Vector2 uvC)
        {
            var triangle = new Triangle3 { V0 = a, V1 = b, V2 = c };
            var normal = triangle.Normal();

            if (!normal.IsFinite())
                return this;


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

        public MeshBuilder AddCircle(Vector3 center, float radius, float subs, bool reverse = false, UVMode uvMode = UVMode.Normalized)
        {
            for (var i = 0; i < subs; i++)
            {
                var a1 = MathF.PI * 2 * i / subs;
                var a2 = MathF.PI * 2 * (i + 1) / subs;
                var v1 = center + new Vector3(MathF.Cos(a1), MathF.Sin(a1), 0) * radius;
                var v2 = center + new Vector3(MathF.Cos(a2), MathF.Sin(a2), 0) * radius;

                Vector2 uv0, uv1, uv2;
      

                if (uvMode == UVMode.Size)
                {
                    uv0 = new Vector2(0, 0);
                    uv1 = new Vector2(MathF.Cos(a1), MathF.Sin(a1)) * radius;
                    uv2 = new Vector2(MathF.Cos(a2), MathF.Sin(a2)) * radius;
                }
                else
                {
                    uv0 = new Vector2(0.5f, 0.5f);
                    uv1 = (new Vector2(MathF.Cos(a1), MathF.Sin(a1)) + Vector2.One) / 2f;
                    uv2 = (new Vector2(MathF.Cos(a2), MathF.Sin(a2)) + Vector2.One) / 2f;
                }

                if (reverse)
                    AddTriangle(v2, center, v1, uv2, uv0, uv1);
                else
                    AddTriangle(v1, center, v2, uv1, uv0, uv2);
            }
            return this;
        }

        public MeshBuilder AddCylinder(Vector3 center, float radius, float height, float subs, UVMode uvMode)
        {
            float u1, u2, vv1, vv2;
            
            if (uvMode == UVMode.Normalized)
            {
                vv1 = 0;
                vv2 = 1;
            }
            else
            {
                vv1 = 0;
                vv2 = height;
            }

            for (var i = 0; i < subs; i++)
            {
                var a1 = MathF.PI * 2 * i / subs;
                var a2 = MathF.PI * 2 * (i + 1) / subs;

                var v1 = center + new Vector3(MathF.Cos(a1) * radius, MathF.Sin(a1) * radius, 0);
                var v2 = center + new Vector3(MathF.Cos(a2) * radius, MathF.Sin(a2) * radius, 0);

                var v3 = new Vector3(v1.X, v1.Y, v1.Z + height);
                var v4 = new Vector3(v2.X, v2.Y, v2.Z + height);


                if (uvMode == UVMode.Normalized)
                {
                    u1 = a1 / (MathF.PI * 2); 
                    u2 = a2 / (MathF.PI * 2);
                }
                else
                {
                    u1 = a1 * radius;
                    u2 = a2 * radius;
                }

                AddFace(v1, v2, v4, v3,
                    new Vector2(u1, vv1),
                    new Vector2(u2, vv1),
                    new Vector2(u2, vv2),
                    new Vector2(u1, vv2));
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

        public MeshBuilder AddRevolve(ICurve2D curve, float subs, UVMode uvMode, bool reverse = false, float startAngle = 0f, float endAngle = MathF.PI * 2)
        {
            var step = (endAngle - startAngle) / subs;


            var samples = curve
                .Sample(0.001f, 1000)
                .ToArray();

            float v1 = 0, v2 = 0, u1, u2;

            for (var i = 0; i < subs; i++)
            {
                var a1 = startAngle + step * i;
                var a2 = startAngle + step * (i + 1);

                var q1 = Quaternion.CreateFromAxisAngle(Vector3.UnitY, a1);
                var q2 = Quaternion.CreateFromAxisAngle(Vector3.UnitY, a2);

                if (uvMode == UVMode.Normalized)
                {
                    v1 = a1 / (MathF.PI * 2);
                    v2 = a2 / (MathF.PI * 2);
                }

                for (var j = 0; j < samples.Length - 1; j++)
                {
                    var s1 = samples[j].Position.ToVector3();
                    var s2 = samples[j + 1].Position.ToVector3();

                    var vr0 = Vector3.Transform(s1, q1);
                    var vr1 = Vector3.Transform(s2, q1);
                    var vr2 = Vector3.Transform(s1, q2);
                    var vr3 = Vector3.Transform(s2, q2);

                    Vector2 uv0, uv1, uv2, uv3;

                    if (uvMode == UVMode.Normalized)
                    {
                        u1 = samples[j].Time;
                        u2 = samples[j + 1].Time;
                        uv0 = new Vector2(u1, v1);
                        uv1 = new Vector2(u2, v1);
                        uv2 = new Vector2(u1, v2);
                        uv3 = new Vector2(u2, v2);
                    }
                    else
                    {
                        u1 = samples[j].Length * 6;
                        u2 = samples[j + 1].Length * 6;
                        var r1 = samples[j].Position.Length();
                        var r2 = samples[j + 1].Position.Length();
                        uv0 = new Vector2(u1, r1 * a1);
                        uv1 = new Vector2(u2,  r2 * a1);
                        uv2 = new Vector2(u1, r1 * a2);
                        uv3 = new Vector2(u2,  r2 * a2);
                    }


                    if (vr0.IsSimilar(vr2))
                    {
                        if (reverse)
                            AddTriangle(vr0, vr1, vr3, uv0, uv1, uv3);
                        else
                            AddTriangle(vr3, vr1, vr0, uv3, uv1, uv0);
                    }
                    else
                        AddFace(vr0, vr1, vr3, vr2, uv0, uv1, uv3, uv2, !reverse);
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

        public MeshBuilder FillCurve(ICurve2D curve, float tolerance = 0.001f, int maxPoints = 1000)
        {
            var samples = curve
                .Sample(tolerance, maxPoints)
                .ToArray();

            return FillPoly(samples.Select(x => x.Position).ToArray());
        }

        public MeshBuilder FillPoly(Vector2[] points)
        {
            var triangles = PolyTriangulate.TriangulateSimplePolygon(points);
            foreach (var triangle in triangles)
                AddTriangle(triangle.V0, triangle.V1, triangle.V2);
            return this;
        }

        public MeshBuilder ExtrudePoly(Vector2[] points, float length, UVMode uvMode, bool addCaps)
        {
            return ExtrudePoly(points, length, Vector3.UnitZ, uvMode, addCaps);
        }


        public MeshBuilder ExtrudePoly(Vector2[] points, float length, Vector3 axis, UVMode uvMode, bool addCaps)
        {
            var ofs = axis * length;

            for (var i = 0; i < points.Length; i++)
            {
                var a = points[i].ToVector3();
                var b = points[(i + 1) % points.Length].ToVector3();
                var c = a + ofs;
                var d = b + ofs;
                AddFace(a, b, d, c);
            }

            if (addCaps)
            {
                var triangles = PolyTriangulate.TriangulateSimplePolygon(points);

                var bounds = points.Bounds();

                Vector2 BoundsUV(Vector3 point)
                {
                    var xy = point.ToVector2() - bounds.Min;
                    if (uvMode == UVMode.Normalized)
                        xy /= bounds.Size;
                    return xy;
                }

                foreach (var triangle in triangles)
                    AddTriangle(triangle.V2, triangle.V1, triangle.V0,
                               BoundsUV(triangle.V2), BoundsUV(triangle.V1), BoundsUV(triangle.V0));

                foreach (var triangle in triangles)
                    AddTriangle(triangle.V0 + ofs, triangle.V1 + ofs, triangle.V2 + ofs,
                               BoundsUV(triangle.V0), BoundsUV(triangle.V1), BoundsUV(triangle.V2));
            }

            return this;
        }

        public MeshBuilder LoftPoly(Poly2 profile, Poly2 path, UVMode uvMode)
        {
            return LoftPoly(profile, new Poly2D(path), uvMode);
        }

        public MeshBuilder LoftPoly(Poly2 profile, ICurve2D path, UVMode uvMode, float tolerance = 0.001f, int maxPoints = 1000)
        {
            var pathPoints = path
                .Sample(tolerance, maxPoints)
                .ToArray();

            Vector3 ToVector3(Vector2 p)
            {
                return new Vector3(p.X, 0, p.Y);
            }

            (Vector3, Vector3) Convert(CurvePoint p)
            {
                return (ToVector3(p.Position), ToVector3(p.Tangent));
            }

            Matrix4x4 Transform(Vector3 position, Vector3 tangent)
            {
                tangent = Vector3.Normalize(tangent);
                var up = new Vector3(0, 1, 0); // Arbitrary "up" vector
                var right = Vector3.Cross(up, tangent); // Perpendicular to tangent
                if (right.LengthSquared() < 1e-6) // Degenerate case: fallback to Z-axis
                    right = Vector3.UnitX;

                up = Vector3.Cross(tangent, right); // Recompute the orthogonal up vector

                return new Matrix4x4(
                    right.X, right.Y, right.Z, 0,
                    up.X, up.Y, up.Z, 0,
                    tangent.X, tangent.Y, tangent.Z, 0,
                    position.X, position.Y, position.Z, 1
                );
            }

            var profLen = profile.Length();

            for (var j = 0; j < pathPoints.Length - 1; j++)
            {
                var (p0, t0) = Convert(pathPoints[j]);
                var (p1, t1) = Convert(pathPoints[j + 1]);

                t1 = (t0 + t1) / 2;

                if (j > 0)
                {
                    var (p2, t2) = Convert(pathPoints[j - 1]);
                    t0 = (t0 + t2) / 2;
                }


                var len = profile.Points.Length;
                if (!profile.IsClosed)
                    len--;

                var matrix0 = Transform(p0, t0);
                var matrix1 = Transform(p1, t1);

                var curLen = 0f;
                for (var i = 0; i < len; i++)
                {
                    var a = profile.Points[i];
                    var b = profile.Points[(i + 1) % profile.Points.Length];

                    var ab = Vector2.Distance(a, b);

                    var a0 = Vector3.Transform(new Vector3(a.X, a.Y, 0), matrix0);
                    var b0 = Vector3.Transform(new Vector3(b.X, b.Y, 0), matrix0);

                    var a1 = Vector3.Transform(new Vector3(a.X, a.Y, 0), matrix1);
                    var b1 = Vector3.Transform(new Vector3(b.X, b.Y, 0), matrix1);

                    Vector2 uva0, uvb0, uva1, uvb1;
                    if (uvMode == UVMode.Normalized)
                    {
                        uva0 = new Vector2(curLen / profLen, pathPoints[j].Time);
                        uva1 = new Vector2(curLen / profLen, pathPoints[j + 1].Time);
                        uvb1 = new Vector2((curLen + ab) / profLen, pathPoints[j + 1].Time);
                        uvb0 = new Vector2((curLen + ab) / profLen, pathPoints[j].Time);
                    }
                    else
                    {
                        uva0 = new Vector2(curLen, pathPoints[j].Length);
                        uva1 = new Vector2(curLen, pathPoints[j + 1].Length);
                        uvb1 = new Vector2((curLen + ab), pathPoints[j + 1].Length);
                        uvb0 = new Vector2((curLen + ab), pathPoints[j].Length);
                    }


                    AddFace(a0, a1, b1, b0,
                        uva0, uva1, uvb1, uvb0,
                        true);

                    curLen += ab;
                }
            }

            return this;
        }

        public MeshBuilder Transform(Matrix4x4 matrix)
        {
            var span = CollectionsMarshal.AsSpan(Vertices);

            foreach (ref var item in span)
            {
                item.Pos = Vector3.Transform(item.Pos, matrix);
                item.Normal = Vector3.TransformNormal(item.Normal, matrix);
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
