using System;
using System.Collections.Generic;
using System.Numerics;

namespace XrEngine
{
    public static class MeshCollapse
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


        /// <summary>
        /// Collapses vertices that are spatially closer than <paramref name="distance"/> and rebuilds the
        /// indexed mesh using the collapsed vertices.
        ///
        /// This is mainly used to turn noisy / duplicated reconstruction output into a real shared-index mesh.
        /// It is especially important before topology-based operations, because triangle-soup geometry can look
        /// connected visually while still having no shared vertices.
        ///
        /// After collapsing, triangles that become degenerate or effectively zero-area are removed, then unused
        /// vertices are compacted.
        /// </summary>
        /// <param name="geometry">
        /// Geometry to modify in place.
        /// The geometry is forced to indexed form before collapsing.
        /// </param>
        /// <param name="distance">
        /// Maximum world-space distance between vertices that should be merged.
        ///
        /// This is the main cleanup strength:
        /// smaller values preserve detail but leave more duplicate/seam vertices;
        /// larger values remove more noise but can weld surfaces that should stay separate.
        ///
        /// Suggested:
        /// around the reconstruction tolerance / voxel noise scale.
        /// For the current reconstruction tests, values around 0.03-0.05 have been useful.
        /// </param>
        /// <param name="cellSize">
        /// Spatial hash cell size used to accelerate neighbor lookup.
        ///
        /// 0 means automatic: <c>distance * 4</c>.
        /// Usually leave this at 0. Set manually only if profiling shows bad bucket distribution.
        /// Too small creates many cells; too large puts too many vertices in each cell.
        /// </param>
        /// <param name="averageUv">
        /// If true, collapsed vertices receive the average UV of all merged vertices.
        /// If false, the collapsed vertex keeps the anchor vertex UV.
        ///
        /// Usually false for meshes where UV islands/seams already matter, because averaging UVs across seams
        /// can corrupt the unwrap/projection data.
        /// Use true only when UVs are temporary or known to be continuous.
        /// </param>
        /// <param name="recomputeNormals">
        /// If true, normals are recomputed after topology changes.
        ///
        /// Usually true after geometric collapse, because merged vertices and removed triangles make old normals
        /// unreliable. Set false only if normals will be recomputed later anyway.
        /// </param>
        /// <param name="areaEpsilon">
        /// Squared cross-product threshold used to remove triangles that become effectively zero-area after
        /// vertex collapse.
        ///
        /// This is not triangle area directly; it is based on <c>LengthSquared(Cross(edge1, edge2))</c>.
        /// Keep very small unless collapsed triangles leave visible needle/degenerate artifacts.
        /// </param>
        /// <param name="recoverSingleTriangleHoles">
        /// If true, does a final conservative recovery pass for triangles removed only by the area filter.
        ///
        /// A removed triangle is restored only when all 3 of its edges already touch alive triangles.
        /// This fills isolated single-triangle holes caused by over-aggressive collapse cleanup without
        /// resurrecting open-border garbage or duplicated-index degenerate triangles.
        /// </param>
        /// <returns>
        /// Counts describing how much geometry was removed by the collapse.
        /// </returns>
        public static CollapseResult CollapseCloseVertices(
            Geometry3D geometry,
            float distance,
            float cellSize = 0,
            bool averageUv = false,
            bool recomputeNormals = true,
            float areaEpsilon = 0.000000000001f,
            bool recoverSingleTriangleHoles = false)
        {
            geometry.EnsureIndices();

            if (cellSize <= 0)
                cellSize = distance * 4.0f;

            var oldVertices = geometry.Vertices;
            var oldIndices = geometry.Indices;

            var vertexIndex = BuildVertexIndex(oldVertices, cellSize);

            var remap = CollapseVertices(
                oldVertices,
                vertexIndex,
                distance,
                cellSize,
                averageUv,
                out var newVertices);

            var newIndices = RebuildTriangles(
                oldIndices,
                remap,
                newVertices,
                areaEpsilon,
                recoverSingleTriangleHoles,
                out var removedZeroAreaTriangles,
                out var recoveredSingleTriangleHoles);

            CompactUsedVertices(ref newVertices, newIndices);

            geometry.Vertices = newVertices;
            geometry.Indices = newIndices;

            if (recomputeNormals)
                geometry.ComputeNormals();

            return new CollapseResult(
                oldVertices.Length,
                newVertices.Length,
                oldIndices.Length / 3,
                newIndices.Length / 3,
                removedZeroAreaTriangles,
                recoveredSingleTriangleHoles);
        }

        private static Dictionary<CellKey, List<int>> BuildVertexIndex(
            VertexData[] vertices,
            float cellSize)
        {
            var result = new Dictionary<CellKey, List<int>>();

            for (var i = 0; i < vertices.Length; i++)
            {
                var cell = GetCell(vertices[i].Pos, cellSize);

                if (!result.TryGetValue(cell, out var list))
                {
                    list = new List<int>();
                    result[cell] = list;
                }

                list.Add(i);
            }

            return result;
        }

        private static uint[] CollapseVertices(
            VertexData[] vertices,
            Dictionary<CellKey, List<int>> vertexIndex,
            float distance,
            float cellSize,
            bool averageUv,
            out VertexData[] newVertices)
        {
            var distanceSq = distance * distance;
            var neighborRadius = Math.Max(1, (int)MathF.Ceiling(distance / cellSize));

            var remap = new uint[vertices.Length];
            Array.Fill(remap, uint.MaxValue);

            var result = new List<VertexData>(vertices.Length);

            for (var i = 0; i < vertices.Length; i++)
            {
                if (remap[i] != uint.MaxValue)
                    continue;

                var anchor = vertices[i];
                var anchorPos = anchor.Pos;
                var anchorCell = GetCell(anchorPos, cellSize);

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

                if (averageUv)
                    collapsed.UV = sumUv / count;

                result.Add(collapsed);
            }

            newVertices = result.ToArray();

            return remap;
        }

        private static uint[] RebuildTriangles(
            uint[] oldIndices,
            uint[] remap,
            VertexData[] vertices,
            float areaEpsilon,
            bool recoverSingleTriangleHoles,
            out int removedZeroAreaTriangles,
            out int recoveredSingleTriangleHoles)
        {
            var triCount = oldIndices.Length / 3;
            var remappedIndices = new uint[oldIndices.Length];
            var alive = new bool[triCount];
            var recoverable = new bool[triCount];

            var removedCandidates = 0;

            for (var tri = 0; tri < triCount; tri++)
            {
                var i = tri * 3;

                var a = remap[oldIndices[i + 0]];
                var b = remap[oldIndices[i + 1]];
                var c = remap[oldIndices[i + 2]];

                remappedIndices[i + 0] = a;
                remappedIndices[i + 1] = b;
                remappedIndices[i + 2] = c;

                if (a == b || b == c || c == a)
                {
                    removedCandidates++;
                    continue;
                }

                var p0 = vertices[a].Pos;
                var p1 = vertices[b].Pos;
                var p2 = vertices[c].Pos;

                var areaSq = Vector3.Cross(p1 - p0, p2 - p0).LengthSquared();

                if (areaSq <= areaEpsilon)
                {
                    removedCandidates++;
                    recoverable[tri] = true;
                    continue;
                }

                alive[tri] = true;
            }

            if (recoverSingleTriangleHoles)
                recoveredSingleTriangleHoles = RecoverSingleTriangleHoles(remappedIndices, alive, recoverable);
            else
                recoveredSingleTriangleHoles = 0;

            removedZeroAreaTriangles = removedCandidates - recoveredSingleTriangleHoles;

            var result = new List<uint>(oldIndices.Length);

            for (var tri = 0; tri < triCount; tri++)
            {
                if (!alive[tri])
                    continue;

                var i = tri * 3;

                result.Add(remappedIndices[i + 0]);
                result.Add(remappedIndices[i + 1]);
                result.Add(remappedIndices[i + 2]);
            }

            return result.ToArray();
        }

        private static int RecoverSingleTriangleHoles(
            uint[] indices,
            bool[] alive,
            bool[] recoverable)
        {
            var triCount = indices.Length / 3;
            var baseAlive = (bool[])alive.Clone();
            var edges = new Dictionary<ulong, List<int>>(triCount * 3);

            for (var tri = 0; tri < triCount; tri++)
            {
                var i = tri * 3;

                AddEdge(edges, indices[i + 0], indices[i + 1], tri);
                AddEdge(edges, indices[i + 1], indices[i + 2], tri);
                AddEdge(edges, indices[i + 2], indices[i + 0], tri);
            }

            var recovered = 0;

            for (var tri = 0; tri < triCount; tri++)
            {
                if (baseAlive[tri])
                    continue;

                if (!recoverable[tri])
                    continue;

                var i = tri * 3;

                if (!HasAliveEdgeNeighbor(edges, baseAlive, indices[i + 0], indices[i + 1], tri))
                    continue;

                if (!HasAliveEdgeNeighbor(edges, baseAlive, indices[i + 1], indices[i + 2], tri))
                    continue;

                if (!HasAliveEdgeNeighbor(edges, baseAlive, indices[i + 2], indices[i + 0], tri))
                    continue;

                alive[tri] = true;
                recovered++;
            }

            return recovered;
        }

        private static void AddEdge(
            Dictionary<ulong, List<int>> edges,
            uint a,
            uint b,
            int tri)
        {
            var key = EdgeKey(a, b);

            if (!edges.TryGetValue(key, out var list))
            {
                list = new List<int>(2);
                edges[key] = list;
            }

            list.Add(tri);
        }

        private static bool HasAliveEdgeNeighbor(
            Dictionary<ulong, List<int>> edges,
            bool[] alive,
            uint a,
            uint b,
            int self)
        {
            if (!edges.TryGetValue(EdgeKey(a, b), out var list))
                return false;

            foreach (var tri in list)
            {
                if (tri != self && alive[tri])
                    return true;
            }

            return false;
        }

        private static ulong EdgeKey(uint a, uint b)
        {
            if (a > b)
                (a, b) = (b, a);

            return ((ulong)a << 32) | b;
        }

        private static void CompactUsedVertices(ref VertexData[] vertices, uint[] indices)
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

        private static CellKey GetCell(Vector3 pos, float cellSize)
        {
            return new CellKey(
                (int)MathF.Floor(pos.X / cellSize),
                (int)MathF.Floor(pos.Y / cellSize),
                (int)MathF.Floor(pos.Z / cellSize));
        }
    }
}