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
                int removedZeroAreaTriangles)
            {
                OldVertexCount = oldVertexCount;
                NewVertexCount = newVertexCount;
                OldTriangleCount = oldTriangleCount;
                NewTriangleCount = newTriangleCount;
                RemovedZeroAreaTriangles = removedZeroAreaTriangles;
            }

            public int OldVertexCount { get; }

            public int NewVertexCount { get; }

            public int OldTriangleCount { get; }

            public int NewTriangleCount { get; }

            public int RemovedZeroAreaTriangles { get; }
        }

        #endregion

        public static CollapseResult CollapseCloseVertices(
            Geometry3D geometry,
            float distance,
            float cellSize = 0,
            bool averageUv = false,
            bool recomputeNormals = true,
            float areaEpsilon = 0.000000000001f)
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
                out var removedZeroAreaTriangles);

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
                removedZeroAreaTriangles);
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
            out int removedZeroAreaTriangles)
        {
            var result = new List<uint>(oldIndices.Length);
            removedZeroAreaTriangles = 0;

            for (var i = 0; i < oldIndices.Length; i += 3)
            {
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

                if (areaSq <= areaEpsilon)
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