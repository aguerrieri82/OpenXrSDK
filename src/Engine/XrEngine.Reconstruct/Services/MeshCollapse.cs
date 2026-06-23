using System;
using System.Collections.Generic;
using System.Numerics;

namespace XrEngine
{
    public sealed class MeshCollapseParams
    {
        public MeshCollapseParams()
        {
            Distance = 0.04f;
            CellSize = 0.0f;
            AverageUv = false;
            RecomputeNormals = true;
            AreaEpsilon = 1E-07f;
            RecoverSingleTriangleHoles = false;
        }

        public float Distance { get; set; }

        public float CellSize { get; set; }

        public bool AverageUv { get; set; }

        public bool RecomputeNormals { get; set; }

        public float AreaEpsilon { get; set; }

        public bool RecoverSingleTriangleHoles { get; set; }
    }

    public sealed class MeshCollapse
    {
        #region Private Structs

        private readonly struct CellKey : IEquatable<CellKey>
        {
            public CellKey(int x, int y, int z)
            {
                X = x;
                Y = y;
                Z = z;
            }

            public bool Equals(CellKey other)
            {
                return X == other.X && Y == other.Y && Z == other.Z;
            }

            public override bool Equals(object? obj)
            {
                return obj is CellKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(X, Y, Z);
            }

            public int X { get; }

            public int Y { get; }

            public int Z { get; }
        }

        private readonly struct TriangleKey : IEquatable<TriangleKey>
        {
            public TriangleKey(uint a, uint b, uint c)
            {
                if (a > b)
                    (a, b) = (b, a);

                if (b > c)
                    (b, c) = (c, b);

                if (a > b)
                    (a, b) = (b, a);

                A = a;
                B = b;
                C = c;
            }

            public bool Equals(TriangleKey other)
            {
                return A == other.A && B == other.B && C == other.C;
            }

            public override bool Equals(object? obj)
            {
                return obj is TriangleKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(A, B, C);
            }

            public uint A { get; }

            public uint B { get; }

            public uint C { get; }
        }

        private struct EdgeInfo
        {
            public EdgeInfo(uint a, uint b, Vector3 normal)
            {
                A = a;
                B = b;
                Count = 1;
                NormalSum = normal;
            }

            public uint A;

            public uint B;

            public int Count;

            public Vector3 NormalSum;
        }

        #endregion

        #region Public Structs

        public readonly struct CollapseResult
        {
            public CollapseResult(
                int oldVertexCount,
                int newVertexCount,
                int oldTriangleCount,
                int newTriangleCount,
                int removedZeroAreaTriangles,
                int recoveredSingleTriangleHoles)
            {
                OldVertexCount = oldVertexCount;
                NewVertexCount = newVertexCount;
                OldTriangleCount = oldTriangleCount;
                NewTriangleCount = newTriangleCount;
                RemovedZeroAreaTriangles = removedZeroAreaTriangles;
                RecoveredSingleTriangleHoles = recoveredSingleTriangleHoles;
            }

            public int OldVertexCount { get; }

            public int NewVertexCount { get; }

            public int OldTriangleCount { get; }

            public int NewTriangleCount { get; }

            public int RemovedZeroAreaTriangles { get; }

            public int RecoveredSingleTriangleHoles { get; }
        }

        #endregion

        private float _distance;
        private float _cellSize;
        private bool _averageUv;
        private bool _recomputeNormals;
        private float _areaEpsilon;
        private bool _recoverSingleTriangleHoles;

        public MeshCollapse()
        {
            SetParameters(new MeshCollapseParams());
        }

        public MeshCollapse(MeshCollapseParams parameters)
        {
            SetParameters(parameters);
        }

        public void SetParameters(MeshCollapseParams parameters)
        {
            _distance = parameters.Distance;
            _cellSize = parameters.CellSize > 0.0f ? parameters.CellSize : parameters.Distance * 4.0f;
            _averageUv = parameters.AverageUv;
            _recomputeNormals = parameters.RecomputeNormals;
            _areaEpsilon = parameters.AreaEpsilon;
            _recoverSingleTriangleHoles = parameters.RecoverSingleTriangleHoles;
        }

        public CollapseResult CollapseCloseVertices(Geometry3D geometry)
        {
            geometry.EnsureIndices();

            var oldVertices = geometry.Vertices;
            var oldIndices = geometry.Indices;

            var vertexIndex = BuildVertexIndex(oldVertices);

            var remap = CollapseVertices(
                oldVertices,
                vertexIndex,
                out var newVertices);

            var newIndices = RebuildTriangles(
                oldIndices,
                remap,
                newVertices,
                out var removedZeroAreaTriangles);

            CompactUsedVertices(ref newVertices, newIndices);

            var recoveredSingleTriangleHoles = 0;

            if (_recoverSingleTriangleHoles)
                recoveredSingleTriangleHoles = RecoverSingleTriangleHoles(newVertices, ref newIndices);

            geometry.Vertices = newVertices;
            geometry.Indices = newIndices;

            if (_recomputeNormals)
                geometry.ComputeNormals();

            return new CollapseResult(
                oldVertices.Length,
                newVertices.Length,
                oldIndices.Length / 3,
                newIndices.Length / 3,
                removedZeroAreaTriangles,
                recoveredSingleTriangleHoles);
        }

        private Dictionary<CellKey, List<int>> BuildVertexIndex(VertexData[] vertices)
        {
            var result = new Dictionary<CellKey, List<int>>();

            for (var i = 0; i < vertices.Length; i++)
            {
                var cell = GetCell(vertices[i].Pos);

                if (!result.TryGetValue(cell, out var list))
                {
                    list = new List<int>();
                    result[cell] = list;
                }

                list.Add(i);
            }

            return result;
        }

        private uint[] CollapseVertices(
            VertexData[] vertices,
            Dictionary<CellKey, List<int>> vertexIndex,
            out VertexData[] newVertices)
        {
            var distanceSq = _distance * _distance;
            var neighborRadius = Math.Max(1, (int)MathF.Ceiling(_distance / _cellSize));

            var remap = new uint[vertices.Length];
            Array.Fill(remap, uint.MaxValue);

            var result = new List<VertexData>(vertices.Length);

            for (var i = 0; i < vertices.Length; i++)
            {
                if (remap[i] != uint.MaxValue)
                    continue;

                var anchor = vertices[i];
                var anchorPos = anchor.Pos;
                var anchorCell = GetCell(anchorPos);
                var newIndex = (uint)result.Count;

                var sumPos = Vector3.Zero;
                var sumNormal = Vector3.Zero;
                var sumUv = Vector2.Zero;
                var count = 0;

                for (var z = -neighborRadius; z <= neighborRadius; z++)
                {
                    for (var y = -neighborRadius; y <= neighborRadius; y++)
                    {
                        for (var x = -neighborRadius; x <= neighborRadius; x++)
                        {
                            var key = new CellKey(
                                anchorCell.X + x,
                                anchorCell.Y + y,
                                anchorCell.Z + z);

                            if (!vertexIndex.TryGetValue(key, out var list))
                                continue;

                            foreach (var oldIndex in list)
                            {
                                if (remap[oldIndex] != uint.MaxValue)
                                    continue;

                                var vertex = vertices[oldIndex];

                                if (Vector3.DistanceSquared(anchorPos, vertex.Pos) > distanceSq)
                                    continue;

                                remap[oldIndex] = newIndex;

                                sumPos += vertex.Pos;
                                sumNormal += vertex.Normal;
                                sumUv += vertex.UV;
                                count++;
                            }
                        }
                    }
                }

                var collapsed = anchor;

                collapsed.Pos = sumPos / count;

                if (sumNormal.LengthSquared() > 0.000001f)
                    collapsed.Normal = Vector3.Normalize(sumNormal);

                if (_averageUv)
                    collapsed.UV = sumUv / count;

                result.Add(collapsed);
            }

            newVertices = result.ToArray();

            return remap;
        }

        private uint[] RebuildTriangles(
            uint[] oldIndices,
            uint[] remap,
            VertexData[] vertices,
            out int removedZeroAreaTriangles)
        {
            var result = new List<uint>(oldIndices.Length);
            var triCount = oldIndices.Length / 3;

            removedZeroAreaTriangles = 0;

            for (var tri = 0; tri < triCount; tri++)
            {
                var i = tri * 3;

                var a = remap[oldIndices[i + 0]];
                var b = remap[oldIndices[i + 1]];
                var c = remap[oldIndices[i + 2]];

                if (a == b || b == c || c == a)
                {
                    removedZeroAreaTriangles++;
                    continue;
                }

                var p0 = vertices[a].Pos;
                var p1 = vertices[b].Pos;
                var p2 = vertices[c].Pos;

                var areaSq = Vector3.Cross(p1 - p0, p2 - p0).LengthSquared();

                if (areaSq <= _areaEpsilon)
                {
                    removedZeroAreaTriangles++;
                    continue;
                }

                result.Add(a);
                result.Add(b);
                result.Add(c);
            }

            return result.ToArray();
        }

        private int RecoverSingleTriangleHoles(VertexData[] vertices, ref uint[] indices)
        {
            var triCount = indices.Length / 3;

            if (triCount == 0)
                return 0;

            var edges = new Dictionary<ulong, EdgeInfo>(triCount * 3);
            var triangles = new HashSet<TriangleKey>();
            var boundaryAdjacency = new Dictionary<uint, List<uint>>();

            for (var tri = 0; tri < triCount; tri++)
            {
                var i = tri * 3;

                var a = indices[i + 0];
                var b = indices[i + 1];
                var c = indices[i + 2];

                triangles.Add(new TriangleKey(a, b, c));

                var normal = GetTriangleNormal(vertices, a, b, c);

                AddEdge(edges, a, b, normal);
                AddEdge(edges, b, c, normal);
                AddEdge(edges, c, a, normal);
            }

            foreach (var item in edges.Values)
            {
                if (item.Count != 1)
                    continue;

                AddBoundaryAdjacency(boundaryAdjacency, item.A, item.B);
                AddBoundaryAdjacency(boundaryAdjacency, item.B, item.A);
            }

            if (boundaryAdjacency.Count == 0)
                return 0;

            var result = new List<uint>(indices.Length + 128 * 3);
            result.AddRange(indices);

            var recovered = 0;

            foreach (var item in edges.Values)
            {
                if (item.Count != 1)
                    continue;

                var a = item.A;
                var b = item.B;

                if (!boundaryAdjacency.TryGetValue(a, out var aEdges))
                    continue;

                foreach (var c in aEdges)
                {
                    if (c == a || c == b)
                        continue;

                    if (!HasBoundaryNeighbor(boundaryAdjacency, b, c))
                        continue;

                    var key = new TriangleKey(a, b, c);

                    if (triangles.Contains(key))
                        continue;

                    var p0 = vertices[a].Pos;
                    var p1 = vertices[b].Pos;
                    var p2 = vertices[c].Pos;

                    var normal = Vector3.Cross(p1 - p0, p2 - p0);

                    if (normal.LengthSquared() <= _areaEpsilon)
                        continue;

                    var neighborNormal =
                        GetEdgeNormal(edges, a, b) +
                        GetEdgeNormal(edges, b, c) +
                        GetEdgeNormal(edges, c, a);

                    triangles.Add(key);

                    if (neighborNormal.LengthSquared() > 0.000001f && Vector3.Dot(normal, neighborNormal) < 0.0f)
                    {
                        result.Add(a);
                        result.Add(c);
                        result.Add(b);
                    }
                    else
                    {
                        result.Add(a);
                        result.Add(b);
                        result.Add(c);
                    }

                    recovered++;
                }
            }

            if (recovered > 0)
                indices = result.ToArray();

            return recovered;
        }

        private void AddEdge(
            Dictionary<ulong, EdgeInfo> edges,
            uint a,
            uint b,
            Vector3 normal)
        {
            var key = EdgeKey(a, b);

            if (edges.TryGetValue(key, out var item))
            {
                item.Count++;
                item.NormalSum += normal;
                edges[key] = item;
                return;
            }

            edges[key] = new EdgeInfo(a, b, normal);
        }

        private void AddBoundaryAdjacency(
            Dictionary<uint, List<uint>> adjacency,
            uint a,
            uint b)
        {
            if (!adjacency.TryGetValue(a, out var list))
            {
                list = new List<uint>(4);
                adjacency[a] = list;
            }

            list.Add(b);
        }

        private bool HasBoundaryNeighbor(
            Dictionary<uint, List<uint>> adjacency,
            uint a,
            uint b)
        {
            if (!adjacency.TryGetValue(a, out var list))
                return false;

            foreach (var item in list)
            {
                if (item == b)
                    return true;
            }

            return false;
        }

        private Vector3 GetEdgeNormal(
            Dictionary<ulong, EdgeInfo> edges,
            uint a,
            uint b)
        {
            if (!edges.TryGetValue(EdgeKey(a, b), out var item))
                return Vector3.Zero;

            return item.NormalSum;
        }

        private Vector3 GetTriangleNormal(
            VertexData[] vertices,
            uint a,
            uint b,
            uint c)
        {
            var p0 = vertices[a].Pos;
            var p1 = vertices[b].Pos;
            var p2 = vertices[c].Pos;

            return Vector3.Cross(p1 - p0, p2 - p0);
        }

        private ulong EdgeKey(uint a, uint b)
        {
            if (a > b)
                (a, b) = (b, a);

            return ((ulong)a << 32) | b;
        }

        private void CompactUsedVertices(ref VertexData[] vertices, uint[] indices)
        {
            var remap = new int[vertices.Length];
            Array.Fill(remap, -1);

            var result = new List<VertexData>(vertices.Length);

            for (var i = 0; i < indices.Length; i++)
            {
                var oldIndex = (int)indices[i];
                var newIndex = remap[oldIndex];

                if (newIndex < 0)
                {
                    newIndex = result.Count;
                    result.Add(vertices[oldIndex]);
                    remap[oldIndex] = newIndex;
                }

                indices[i] = (uint)newIndex;
            }

            vertices = result.ToArray();
        }

        private CellKey GetCell(Vector3 pos)
        {
            return new CellKey(
                (int)MathF.Floor(pos.X / _cellSize),
                (int)MathF.Floor(pos.Y / _cellSize),
                (int)MathF.Floor(pos.Z / _cellSize));
        }
    }
}