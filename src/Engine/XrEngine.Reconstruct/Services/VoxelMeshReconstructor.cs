using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

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

        /// <summary>
        /// Spatial resolution of the fused reconstruction grid, in world units/meters.
        ///
        /// This is the main geometry/detail knob:
        /// smaller values preserve more detail but create more voxels, more vertices, more noise and slower extraction;
        /// larger values produce a smoother/coarser mesh and absorb more frame-to-frame depth jitter.
        ///
        /// Suggested:
        /// 0.03-0.05 for detailed room/object reconstruction;
        /// 0.08-0.10 for fast/debug/coarser reconstruction.
        /// </summary>
        public float VoxelSize { get; set; }

        /// <summary>
        /// Signed-distance fusion band around each observed surface, in world units/meters.
        ///
        /// A depth sample does not affect only one infinitely thin surface point: it contributes evidence
        /// within this distance around the measured surface. Larger values make different captures blend more
        /// easily and fill small inconsistencies, but can thicken surfaces or smear close parallel geometry.
        ///
        /// Usually keep this around 2-4 times VoxelSize.
        /// </summary>
        public float TruncationDistance { get; set; }

        /// <summary>
        /// Minimum number of supporting observations required before extracted geometry is trusted.
        ///
        /// 1 keeps every observed surface and maximizes coverage, but also keeps single-frame noise.
        /// Higher values remove weak/noisy geometry, but can delete areas seen by only one capture.
        ///
        /// Suggested:
        /// 1 for maximum coverage/debug;
        /// 2+ only when captures overlap enough and isolated depth noise is a real problem.
        /// </summary>
        public int MinObservations { get; set; }

        /// <summary>
        /// Optional maximum triangle edge length after extraction, in world units/meters.
        ///
        /// 0 disables this filter.
        /// Use it to reject stretched triangles created across depth discontinuities or poorly supported
        /// voxel transitions. Too low will punch holes in valid large/flat surfaces.
        ///
        /// Suggested:
        /// 0 disabled while tuning reconstruction;
        /// about 2-4 times VoxelSize if long-edge artifacts appear.
        /// </summary>
        public float MaxTriangleEdge { get; set; }
    }

    public sealed class VoxelMeshReconstructor
    {
        #region Private Structs

        private readonly struct VoxelKey : IEquatable<VoxelKey>
        {
            public VoxelKey(int x, int y, int z)
            {
                X = x;
                Y = y;
                Z = z;
            }

            public bool Equals(VoxelKey other)
            {
                return X == other.X && Y == other.Y && Z == other.Z;
            }

            public override bool Equals(object? obj)
            {
                return obj is VoxelKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (int)(
                        (uint)X * 73856093u ^
                        (uint)Y * 19349663u ^
                        (uint)Z * 83492791u);
                }
            }

            public readonly int X;
            public readonly int Y;
            public readonly int Z;
        }

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

        private static readonly byte[] Tetrahedra =
        {
            0, 5, 1, 6,
            0, 1, 2, 6,
            0, 2, 3, 6,
            0, 3, 7, 6,
            0, 7, 4, 6,
            0, 4, 5, 6
        };

        private readonly Dictionary<VoxelKey, Voxel> _voxels;
        private readonly HashSet<VoxelKey> _activeCells;

        private float _truncationDistance;
        private float _truncationDistanceInv;
        private float _truncationDistanceSq;
        private float _voxelSize;
        private float _voxelSizeInv;
        private Vector128<float> _voxelSizeInvVec;
        private int _minObservations;
        private float _maxTriangleEdge;
        private float _maxTriangleEdgeSq;

        public VoxelMeshReconstructor()
        {
            SetParams(new VoxelMeshReconstructorParams());

            var roomVolume = 5.0f * 5.0f * 5.0f;
            var voxelVolume = _voxelSize * _voxelSize * _voxelSize;
            var size = (int)(roomVolume / voxelVolume * 0.2f);

            _activeCells = new HashSet<VoxelKey>(size);
            _voxels = new Dictionary<VoxelKey, Voxel>(size);
        }

        public void SetParams(VoxelMeshReconstructorParams parameters)
        {
            _voxelSize = parameters.VoxelSize;
            _voxelSizeInv = 1.0f / _voxelSize;
            _voxelSizeInvVec = Vector128.Create(_voxelSizeInv);

            _truncationDistance = parameters.TruncationDistance;
            _truncationDistanceInv = 1.0f / _truncationDistance;
            _truncationDistanceSq = _truncationDistance * _truncationDistance;

            _minObservations = parameters.MinObservations;

            _maxTriangleEdge = parameters.MaxTriangleEdge;
            _maxTriangleEdgeSq = _maxTriangleEdge * _maxTriangleEdge;
        }

        public void Reset()
        {
            _voxels.Clear();
            _activeCells.Clear();
        }

        public unsafe void FeedFrame(Geometry3D geometry)
        {
            var vertices = geometry.Vertices!;
            var indices = geometry.Indices!;

            fixed (uint* pIndices = &indices[0])
            fixed (VertexData* pVertices = &vertices[0])
            {
                for (var i = 0; i + 2 < indices.Length; i += 3)
                {
                    IntegrateTriangle(
                        pVertices[(int)pIndices[i + 0]].Pos,
                        pVertices[(int)pIndices[i + 1]].Pos,
                        pVertices[(int)pIndices[i + 2]].Pos);
                }
            }
        }

        public unsafe void FeedFrame(Geometry3D geometry, Matrix4x4 transform)
        {
            var vertices = geometry.Vertices!;
            var indices = geometry.Indices!;

            fixed (uint* pIndices = &indices[0])
            fixed (VertexData* pVertices = &vertices[0])
            {
                for (var i = 0; i + 2 < indices.Length; i += 3)
                {
                    IntegrateTriangle(
                        Vector3.Transform(pVertices[(int)pIndices[i + 0]].Pos, transform),
                        Vector3.Transform(pVertices[(int)pIndices[i + 1]].Pos, transform),
                        Vector3.Transform(pVertices[(int)pIndices[i + 2]].Pos, transform));
                }
            }
        }

        public unsafe Geometry3D ExtractMesh(Geometry3D output)
        {
            var estimatedVertexCount = Math.Min(_activeCells.Count * 6, 8_000_000);

            var vertices = new List<VertexData>(estimatedVertexCount);
            var indices = new List<uint>(estimatedVertexCount);

            var corners = stackalloc Corner[8];

            foreach (var key in _activeCells)
            {
                if (!TryReadCube(key, corners))
                    continue;

                for (var i = 0; i < Tetrahedra.Length; i += 4)
                {
                    EmitTetrahedron(
                        corners,
                        Tetrahedra[i + 0],
                        Tetrahedra[i + 1],
                        Tetrahedra[i + 2],
                        Tetrahedra[i + 3],
                        vertices,
                        indices);
                }
            }

            output.Vertices = vertices.ToArray();
            output.Indices = indices.ToArray();

            output.ActiveComponents =
                VertexComponent.Position |
                VertexComponent.Normal;

            // Normals are already interpolated from voxel normals.
            // Call ComputeNormals() here only if you prefer face-derived mesh normals.
            output.UpdateBounds();

            return output;
        }

        private void IntegrateTriangle(Vector3 a, Vector3 b, Vector3 c)
        {
            var ab = b - a;
            var ac = c - a;
            var bc = c - b;

            if (_maxTriangleEdge > 0.0f)
            {
                if (ab.LengthSquared() > _maxTriangleEdgeSq ||
                    bc.LengthSquared() > _maxTriangleEdgeSq ||
                    (a - c).LengthSquared() > _maxTriangleEdgeSq)
                    return;
            }

            var normal = Vector3.Cross(ab, ac);
            var normalLenSq = normal.LengthSquared();

            if (normalLenSq <= 0.000000000001f)
                return;

            normal *= 1.0f / MathF.Sqrt(normalLenSq);

            var trunc = new Vector3(_truncationDistance);

            var min = Vector3.Min(a, Vector3.Min(b, c)) - trunc;
            var max = Vector3.Max(a, Vector3.Max(b, c)) + trunc;

            var minKey = ToKey(min);
            var maxKey = ToKey(max);

            var voxelSize = _voxelSize;
            var truncationDistance = _truncationDistance;
            var truncationDistanceSq = _truncationDistanceSq;
            var truncationDistanceInv = _truncationDistanceInv;

            for (var z = minKey.Z; z <= maxKey.Z; z++)
            {
                var pz = (z + 0.5f) * voxelSize;

                for (var y = minKey.Y; y <= maxKey.Y; y++)
                {
                    var py = (y + 0.5f) * voxelSize;

                    for (var x = minKey.X; x <= maxKey.X; x++)
                    {
                        var px = (x + 0.5f) * voxelSize;
                        var p = new Vector3(px, py, pz);

                        var planeDistance = Vector3.Dot(p - a, normal);

                        if (planeDistance < -truncationDistance ||
                            planeDistance > truncationDistance)
                            continue;

                        var closest = ClosestPointOnTriangle(p, a, b, c, ab, ac, bc);

                        if ((p - closest).LengthSquared() > truncationDistanceSq)
                            continue;

                        var tsdf = planeDistance * truncationDistanceInv;

                        if (tsdf < -1.0f)
                            tsdf = -1.0f;
                        else if (tsdf > 1.0f)
                            tsdf = 1.0f;

                        IntegrateVoxel(new VoxelKey(x, y, z), tsdf, normal);
                    }
                }
            }
        }

        private void IntegrateVoxel(VoxelKey key, float distance, Vector3 normal)
        {
            ref var voxel = ref CollectionsMarshal.GetValueRefOrAddDefault(_voxels, key, out var exists);

            var oldWeight = voxel.Weight;
            var newWeight = oldWeight + 1.0f;

            voxel.Distance = (voxel.Distance * oldWeight + distance) / newWeight;
            voxel.Weight = newWeight;
            voxel.NormalSum += normal;

            if (exists)
                return;

            _activeCells.Add(new VoxelKey(key.X - 1, key.Y - 1, key.Z - 1));
            _activeCells.Add(new VoxelKey(key.X + 0, key.Y - 1, key.Z - 1));
            _activeCells.Add(new VoxelKey(key.X - 1, key.Y + 0, key.Z - 1));
            _activeCells.Add(new VoxelKey(key.X + 0, key.Y + 0, key.Z - 1));
            _activeCells.Add(new VoxelKey(key.X - 1, key.Y - 1, key.Z + 0));
            _activeCells.Add(new VoxelKey(key.X + 0, key.Y - 1, key.Z + 0));
            _activeCells.Add(new VoxelKey(key.X - 1, key.Y + 0, key.Z + 0));
            _activeCells.Add(new VoxelKey(key.X + 0, key.Y + 0, key.Z + 0));
        }

        private unsafe bool TryReadCube(VoxelKey key, Corner* corners)
        {
            if (!TryReadCorner(new VoxelKey(key.X + 0, key.Y + 0, key.Z + 0), out corners[0])) return false;
            if (!TryReadCorner(new VoxelKey(key.X + 1, key.Y + 0, key.Z + 0), out corners[1])) return false;
            if (!TryReadCorner(new VoxelKey(key.X + 1, key.Y + 1, key.Z + 0), out corners[2])) return false;
            if (!TryReadCorner(new VoxelKey(key.X + 0, key.Y + 1, key.Z + 0), out corners[3])) return false;
            if (!TryReadCorner(new VoxelKey(key.X + 0, key.Y + 0, key.Z + 1), out corners[4])) return false;
            if (!TryReadCorner(new VoxelKey(key.X + 1, key.Y + 0, key.Z + 1), out corners[5])) return false;
            if (!TryReadCorner(new VoxelKey(key.X + 1, key.Y + 1, key.Z + 1), out corners[6])) return false;
            if (!TryReadCorner(new VoxelKey(key.X + 0, key.Y + 1, key.Z + 1), out corners[7])) return false;

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryReadCorner(VoxelKey key, out Corner corner)
        {
            if (!_voxels.TryGetValue(key, out var voxel))
            {
                corner = default;
                return false;
            }

            if (voxel.Weight < _minObservations)
            {
                corner = default;
                return false;
            }

            var normal = voxel.NormalSum;
            var normalLenSq = normal.LengthSquared();

            if (normalLenSq > 0.000000000001f)
                normal *= 1.0f / MathF.Sqrt(normalLenSq);

            corner = new Corner
            {
                Pos = VoxelCenter(key),
                Normal = normal,
                Distance = voxel.Distance
            };

            return true;
        }

        private static unsafe void EmitTetrahedron(
            Corner* cube,
            int t0,
            int t1,
            int t2,
            int t3,
            List<VertexData> vertices,
            List<uint> indices)
        {
            var inside = stackalloc int[4];
            var outside = stackalloc int[4];

            var insideCount = 0;
            var outsideCount = 0;

            AddTetraIndex(cube, t0, inside, outside, ref insideCount, ref outsideCount);
            AddTetraIndex(cube, t1, inside, outside, ref insideCount, ref outsideCount);
            AddTetraIndex(cube, t2, inside, outside, ref insideCount, ref outsideCount);
            AddTetraIndex(cube, t3, inside, outside, ref insideCount, ref outsideCount);

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void AddTetraIndex(
            Corner* cube,
            int index,
            int* inside,
            int* outside,
            ref int insideCount,
            ref int outsideCount)
        {
            if (cube[index].Distance < 0.0f)
                inside[insideCount++] = index;
            else
                outside[outsideCount++] = index;
        }

        private static VertexData Interpolate(Corner a, Corner b)
        {
            var denom = a.Distance - b.Distance;
            var t = MathF.Abs(denom) <= 0.000001f
                ? 0.5f
                : a.Distance / denom;

            if (t < 0.0f)
                t = 0.0f;
            else if (t > 1.0f)
                t = 1.0f;

            var normal = Vector3.Lerp(a.Normal, b.Normal, t);
            var normalLenSq = normal.LengthSquared();

            if (normalLenSq > 0.000000000001f)
                normal *= 1.0f / MathF.Sqrt(normalLenSq);

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

            if (refNormal.LengthSquared() > 0.0000000001f &&
                Vector3.Dot(faceNormal, refNormal) < 0.0f)
            {
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private VoxelKey ToKey(Vector3 p)
        {
            var vec = p.AsVector128Unsafe();

            vec = Vector128.Multiply(vec, _voxelSizeInvVec);
            vec = Vector128.Floor(vec);

            var iVec = Vector128.ConvertToInt32(vec);

            return new VoxelKey(iVec[0], iVec[1], iVec[2]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Vector3 VoxelCenter(VoxelKey key)
        {
            return new Vector3(
                (key.X + 0.5f) * _voxelSize,
                (key.Y + 0.5f) * _voxelSize,
                (key.Z + 0.5f) * _voxelSize);
        }

        private static Vector3 ClosestPointOnTriangle(
            Vector3 p,
            Vector3 a,
            Vector3 b,
            Vector3 c,
            Vector3 ab,
            Vector3 ac,
            Vector3 bc)
        {
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
                return a + ab * v;
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
                return a + ac * w;
            }

            var va = d3 * d6 - d5 * d4;

            if (va <= 0.0f && d4 - d3 >= 0.0f && d5 - d6 >= 0.0f)
            {
                var w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
                return b + bc * w;
            }

            var denom = 1.0f / (va + vb + vc);
            var v2 = vb * denom;
            var w2 = vc * denom;

            return a + ab * v2 + ac * w2;
        }

        public float VoxelSize => _voxelSize;

        public float TruncationDistance => _truncationDistance;

        public int MinObservations => _minObservations;

        public float MaxTriangleEdge => _maxTriangleEdge;
    }
}