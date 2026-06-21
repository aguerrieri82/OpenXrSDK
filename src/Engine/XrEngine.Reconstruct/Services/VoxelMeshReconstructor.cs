using System;
using System.Collections.Generic;
using System.Numerics;

namespace XrEngine.Reconstruct
{
    public class VoxelMeshReconstructorParams
    {
        public VoxelMeshReconstructorParams()
        {
            VoxelSize = 0.1f;
            TruncationDistance = 0.15f;
            MinObservations = 1;
            MaxTriangleEdge = 0.0f;
        }

        public float VoxelSize { get; set; }

        public float TruncationDistance { get; set; }

        public int MinObservations { get; set; }

        public float MaxTriangleEdge { get; set; }
    }

    public sealed class VoxelMeshReconstructor
    {
        #region Private Structs

        private readonly record struct VoxelKey(int X, int Y, int Z);

        private struct Voxel
        {
            public float Distance;
            public float Weight;
            public Vector3 NormalSum;
        }

        private struct Corner
        {
            public Vector3 Pos;
            public Vector3 Normal;
            public float Distance;
        }

        #endregion

        private static readonly VoxelKey[] CornerOffsets =
        {
            new(0, 0, 0),
            new(1, 0, 0),
            new(1, 1, 0),
            new(0, 1, 0),
            new(0, 0, 1),
            new(1, 0, 1),
            new(1, 1, 1),
            new(0, 1, 1)
        };

        private static readonly int[][] Tetrahedra =
        {
            new[] { 0, 5, 1, 6 },
            new[] { 0, 1, 2, 6 },
            new[] { 0, 2, 3, 6 },
            new[] { 0, 3, 7, 6 },
            new[] { 0, 7, 4, 6 },
            new[] { 0, 4, 5, 6 }
        };

        private readonly Dictionary<VoxelKey, Voxel> _voxels;

        private readonly HashSet<VoxelKey> _activeCells;

        public VoxelMeshReconstructor()
        {
            _voxels = new Dictionary<VoxelKey, Voxel>();
            _activeCells = new HashSet<VoxelKey>();

            SetParams(new VoxelMeshReconstructorParams());
        }

        public void SetParams(VoxelMeshReconstructorParams parameters)
        {
            VoxelSize = parameters.VoxelSize;
            TruncationDistance = parameters.TruncationDistance;
            MinObservations = parameters.MinObservations;
            MaxTriangleEdge = parameters.MaxTriangleEdge;
        }

        public void Reset()
        {
            _voxels.Clear();
            _activeCells.Clear();
        }

        public void FeedFrame(Geometry3D geometry)
        {
            FeedFrame(geometry, Matrix4x4.Identity);
        }

        public void FeedFrame(Geometry3D geometry, Matrix4x4 transform)
        {
            var vertices = geometry.Vertices;
            if (vertices == null || vertices.Length < 3)
                return;

            var indices = geometry.Indices;

            if (indices != null && indices.Length >= 3)
            {
                for (var i = 0; i + 2 < indices.Length; i += 3)
                {
                    var ia = (int)indices[i + 0];
                    var ib = (int)indices[i + 1];
                    var ic = (int)indices[i + 2];

                    if ((uint)ia >= vertices.Length ||
                        (uint)ib >= vertices.Length ||
                        (uint)ic >= vertices.Length)
                        continue;

                    IntegrateTriangle(
                        vertices[ia],
                        vertices[ib],
                        vertices[ic],
                        transform);
                }
            }
            else
            {
                for (var i = 0; i + 2 < vertices.Length; i += 3)
                {
                    IntegrateTriangle(
                        vertices[i + 0],
                        vertices[i + 1],
                        vertices[i + 2],
                        transform);
                }
            }
        }

        public Geometry3D ExtractMesh(Geometry3D output)
        {
            var vertices = new List<VertexData>();
            var indices = new List<uint>();

            var corners = new Corner[8];

            foreach (var key in _activeCells)
            {
                if (!TryReadCube(key, corners))
                    continue;

                for (var i = 0; i < Tetrahedra.Length; i++)
                    EmitTetrahedron(corners, Tetrahedra[i], vertices, indices);
            }

            output.Vertices = vertices.ToArray();
            output.Indices = indices.ToArray();

            output.ActiveComponents =
                VertexComponent.Position |
                VertexComponent.Normal;

            output.ComputeNormals();
            output.UpdateBounds();

            return output;
        }

        private void IntegrateTriangle(
            VertexData va,
            VertexData vb,
            VertexData vc,
            Matrix4x4 transform)
        {
            var a = Vector3.Transform(va.Pos, transform);
            var b = Vector3.Transform(vb.Pos, transform);
            var c = Vector3.Transform(vc.Pos, transform);

            if (MaxTriangleEdge > 0.0f)
            {
                if (Vector3.Distance(a, b) > MaxTriangleEdge ||
                    Vector3.Distance(b, c) > MaxTriangleEdge ||
                    Vector3.Distance(c, a) > MaxTriangleEdge)
                    return;
            }

            var normal = Vector3.Cross(b - a, c - a);
            var normalLen = normal.Length();

            if (normalLen <= 0.000001f)
                return;

            normal /= normalLen;

            var min = Vector3.Min(a, Vector3.Min(b, c)) - new Vector3(TruncationDistance);
            var max = Vector3.Max(a, Vector3.Max(b, c)) + new Vector3(TruncationDistance);

            var minKey = ToKey(min);
            var maxKey = ToKey(max);

            for (var z = minKey.Z; z <= maxKey.Z; z++)
            {
                for (var y = minKey.Y; y <= maxKey.Y; y++)
                {
                    for (var x = minKey.X; x <= maxKey.X; x++)
                    {
                        var key = new VoxelKey(x, y, z);
                        var p = VoxelCenter(key);

                        var closest = ClosestPointOnTriangle(p, a, b, c);
                        var distanceToTriangle = Vector3.Distance(p, closest);

                        if (distanceToTriangle > TruncationDistance)
                            continue;

                        var signedDistance = Vector3.Dot(p - closest, normal);
                        var tsdf = Math.Clamp(signedDistance / TruncationDistance, -1.0f, 1.0f);

                        IntegrateVoxel(key, tsdf, normal);
                    }
                }
            }
        }

        private void IntegrateVoxel(VoxelKey key, float distance, Vector3 normal)
        {
            _voxels.TryGetValue(key, out var voxel);

            var oldWeight = voxel.Weight;
            var newWeight = oldWeight + 1.0f;

            voxel.Distance = (voxel.Distance * oldWeight + distance) / newWeight;
            voxel.Weight = newWeight;
            voxel.NormalSum += normal;

            _voxels[key] = voxel;

            for (var z = -1; z <= 0; z++)
            {
                for (var y = -1; y <= 0; y++)
                {
                    for (var x = -1; x <= 0; x++)
                    {
                        _activeCells.Add(new VoxelKey(
                            key.X + x,
                            key.Y + y,
                            key.Z + z));
                    }
                }
            }
        }

        private bool TryReadCube(VoxelKey key, Corner[] corners)
        {
            for (var i = 0; i < 8; i++)
            {
                var offset = CornerOffsets[i];

                var voxelKey = new VoxelKey(
                    key.X + offset.X,
                    key.Y + offset.Y,
                    key.Z + offset.Z);

                if (!_voxels.TryGetValue(voxelKey, out var voxel))
                    return false;

                if (voxel.Weight < MinObservations)
                    return false;

                var normal = voxel.NormalSum;
                var normalLen = normal.Length();

                if (normalLen > 0.000001f)
                    normal /= normalLen;

                corners[i] = new Corner
                {
                    Pos = VoxelCenter(voxelKey),
                    Normal = normal,
                    Distance = voxel.Distance
                };
            }

            return true;
        }

        private void EmitTetrahedron(
    Corner[] cube,
    int[] tetrahedron,
    List<VertexData> vertices,
    List<uint> indices)
        {
            var inside = new int[4];
            var outside = new int[4];

            var insideCount = 0;
            var outsideCount = 0;

            for (var i = 0; i < 4; i++)
            {
                var index = tetrahedron[i];

                if (cube[index].Distance < 0.0f)
                    inside[insideCount++] = index;
                else
                    outside[outsideCount++] = index;
            }

            if (insideCount == 0 || insideCount == 4)
                return;

            if (insideCount == 1)
            {
                var a = inside[0];

                var v0 = Interpolate(cube[a], cube[outside[0]]);
                var v1 = Interpolate(cube[a], cube[outside[1]]);
                var v2 = Interpolate(cube[a], cube[outside[2]]);

                EmitTriangle(vertices, indices, v0, v1, v2);
                return;
            }

            if (insideCount == 3)
            {
                var a = outside[0];

                var v0 = Interpolate(cube[a], cube[inside[0]]);
                var v1 = Interpolate(cube[a], cube[inside[1]]);
                var v2 = Interpolate(cube[a], cube[inside[2]]);

                EmitTriangle(vertices, indices, v0, v2, v1);
                return;
            }

            var i0 = inside[0];
            var i1 = inside[1];
            var o0 = outside[0];
            var o1 = outside[1];

            var q0 = Interpolate(cube[i0], cube[o0]);
            var q1 = Interpolate(cube[i0], cube[o1]);
            var q2 = Interpolate(cube[i1], cube[o0]);
            var q3 = Interpolate(cube[i1], cube[o1]);

            EmitTriangle(vertices, indices, q0, q1, q2);
            EmitTriangle(vertices, indices, q2, q1, q3);
        }

        private static VertexData Interpolate(Corner a, Corner b)
        {
            var denom = a.Distance - b.Distance;
            var t = MathF.Abs(denom) <= 0.000001f
                ? 0.5f
                : a.Distance / denom;

            t = Math.Clamp(t, 0.0f, 1.0f);

            var normal = Vector3.Lerp(a.Normal, b.Normal, t);
            var normalLen = normal.Length();

            if (normalLen > 0.000001f)
                normal /= normalLen;

            return new VertexData
            {
                Pos = Vector3.Lerp(a.Pos, b.Pos, t),
                Normal = normal
            };
        }

        private static void EmitTriangle(
            List<VertexData> vertices,
            List<uint> indices,
            VertexData a,
            VertexData b,
            VertexData c)
        {
            var ab = b.Pos - a.Pos;
            var ac = c.Pos - a.Pos;

            var faceNormal = Vector3.Cross(ab, ac);

            if (faceNormal.LengthSquared() <= 0.0000000001f)
                return;

            var refNormal = a.Normal + b.Normal + c.Normal;

            if (refNormal.LengthSquared() > 0.0000000001f)
            {
                if (Vector3.Dot(faceNormal, refNormal) < 0.0f)
                    (b, c) = (c, b);
            }

            var start = (uint)vertices.Count;

            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);

            indices.Add(start + 0);
            indices.Add(start + 1);
            indices.Add(start + 2);
        }

        private VoxelKey ToKey(Vector3 p)
        {
            var s = 1.0f / VoxelSize;

            return new VoxelKey(
                (int)MathF.Floor(p.X * s),
                (int)MathF.Floor(p.Y * s),
                (int)MathF.Floor(p.Z * s));
        }

        private Vector3 VoxelCenter(VoxelKey key)
        {
            return new Vector3(
                (key.X + 0.5f) * VoxelSize,
                (key.Y + 0.5f) * VoxelSize,
                (key.Z + 0.5f) * VoxelSize);
        }

        private static Vector3 ClosestPointOnTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            var ab = b - a;
            var ac = c - a;
            var ap = p - a;

            var d1 = Vector3.Dot(ab, ap);
            var d2 = Vector3.Dot(ac, ap);

            if (d1 <= 0.0f && d2 <= 0.0f)
                return a;

            var bp = p - b;
            var d3 = Vector3.Dot(ab, bp);
            var d4 = Vector3.Dot(ac, bp);

            if (d3 >= 0.0f && d4 <= d3)
                return b;

            var vc = d1 * d4 - d3 * d2;
            if (vc <= 0.0f && d1 >= 0.0f && d3 <= 0.0f)
            {
                var v = d1 / (d1 - d3);
                return a + v * ab;
            }

            var cp = p - c;
            var d5 = Vector3.Dot(ab, cp);
            var d6 = Vector3.Dot(ac, cp);

            if (d6 >= 0.0f && d5 <= d6)
                return c;

            var vb = d5 * d2 - d1 * d6;
            if (vb <= 0.0f && d2 >= 0.0f && d6 <= 0.0f)
            {
                var w = d2 / (d2 - d6);
                return a + w * ac;
            }

            var va = d3 * d6 - d5 * d4;
            if (va <= 0.0f && d4 - d3 >= 0.0f && d5 - d6 >= 0.0f)
            {
                var w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
                return b + w * (c - b);
            }

            var denom = 1.0f / (va + vb + vc);
            var v2 = vb * denom;
            var w2 = vc * denom;

            return a + ab * v2 + ac * w2;
        }

        public float VoxelSize { get; set; }

        public float TruncationDistance { get; set; }

        public int MinObservations { get; set; }

        public float MaxTriangleEdge { get; set; }
    }
}