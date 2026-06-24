using System;
using System.Collections.Generic;
using System.Numerics;

namespace XrEngine
{
    public sealed class MeshUvSingleTriangleHoleFillParams
    {
        public MeshUvSingleTriangleHoleFillParams()
        {
            AtlasSize = 4096;
            UvMergePixels = 1.5f;
            Passes = 1;
            MinTriangleArea = 0.0f;
            MaxEdgeLength = 0.0f;
            RecomputeNormals = false;
        }

        public int AtlasSize { get; set; }

        public float UvMergePixels { get; set; }

        public int Passes { get; set; }

        public float MinTriangleArea { get; set; }

        public float MaxEdgeLength { get; set; }

        public bool RecomputeNormals { get; set; }
    }

    public sealed class MeshUvSingleTriangleHoleFiller
    {
        #region Private Structs

        private readonly struct CellKey : IEquatable<CellKey>
        {
            public CellKey(int x, int y)
            {
                X = x;
                Y = y;
            }

            public bool Equals(CellKey other)
            {
                return X == other.X && Y == other.Y;
            }

            public override bool Equals(object? obj)
            {
                return obj is CellKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (X * 397) ^ Y;
                }
            }

            public int X { get; }

            public int Y { get; }
        }

        private readonly struct EdgeKey : IEquatable<EdgeKey>
        {
            public EdgeKey(int a, int b)
            {
                if (a < b)
                {
                    A = a;
                    B = b;
                }
                else
                {
                    A = b;
                    B = a;
                }
            }

            public bool Equals(EdgeKey other)
            {
                return A == other.A && B == other.B;
            }

            public override bool Equals(object? obj)
            {
                return obj is EdgeKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (A * 397) ^ B;
                }
            }

            public int A { get; }

            public int B { get; }
        }

        private readonly struct TriangleKey : IEquatable<TriangleKey>
        {
            public TriangleKey(int a, int b, int c)
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
                unchecked
                {
                    var hash = A;
                    hash = (hash * 397) ^ B;
                    hash = (hash * 397) ^ C;
                    return hash;
                }
            }

            public int A { get; }

            public int B { get; }

            public int C { get; }
        }

        private struct UvPoint
        {
            public Vector2 Uv;
            public Vector2 Pixel;
            public uint RepresentativeIndex;
        }

        private struct UvTriangle
        {
            public int A;
            public int B;
            public int C;
            public int Component;
            public float SignedArea;
        }

        private struct EdgeInfo
        {
            public int A;
            public int B;
            public int Count;
        }

        #endregion

        #region Public Structs

        public readonly struct FillResult
        {
            public FillResult(int addedTriangles, int passes)
            {
                AddedTriangles = addedTriangles;
                Passes = passes;
            }

            public int AddedTriangles { get; }

            public int Passes { get; }
        }

        #endregion

        private const float UvAreaEpsilon = 1E-12f;
        private const float GeometryAreaEpsilon = 1E-12f;

        private int _atlasSize;
        private float _uvMergePixels;
        private int _passes;
        private float _minTriangleArea;
        private float _maxEdgeLength;
        private bool _recomputeNormals;

        public MeshUvSingleTriangleHoleFiller()
        {
            SetParameters(new MeshUvSingleTriangleHoleFillParams());
        }

        public MeshUvSingleTriangleHoleFiller(MeshUvSingleTriangleHoleFillParams parameters)
        {
            SetParameters(parameters);
        }

        public void SetParameters(MeshUvSingleTriangleHoleFillParams parameters)
        {
            _atlasSize = parameters.AtlasSize;
            _uvMergePixels = parameters.UvMergePixels;
            _passes = parameters.Passes;
            _minTriangleArea = parameters.MinTriangleArea;
            _maxEdgeLength = parameters.MaxEdgeLength;
            _recomputeNormals = parameters.RecomputeNormals;
        }

        public FillResult Fill(Geometry3D geometry)
        {
            geometry.EnsureIndices();

            var vertices = geometry.Vertices;
            var indices = new List<uint>(geometry.Indices.Length + 256);

            for (var i = 0; i < geometry.Indices.Length; i++)
                indices.Add(geometry.Indices[i]);

            var totalAdded = 0;
            var executedPasses = 0;

            for (var pass = 0; pass < _passes; pass++)
            {
                var added = FillPass(vertices, indices);

                executedPasses++;
                totalAdded += added;

                Log.Info(
                    this,
                    "MeshUvSingleTriangleHoleFiller pass={0}: added={1}",
                    pass + 1,
                    added);

                if (added == 0)
                    break;
            }

            if (totalAdded > 0)
            {
                geometry.Indices = indices.ToArray();

                if (_recomputeNormals)
                    geometry.ComputeNormals();
            }

            Log.Info(
                this,
                "MeshUvSingleTriangleHoleFiller done: added={0} passes={1}",
                totalAdded,
                executedPasses);

            return new FillResult(totalAdded, executedPasses);
        }

        private int FillPass(VertexData[] vertices, List<uint> indices)
        {
            var points = BuildUvPoints(vertices, out var vertexToPoint);
            var parents = new int[points.Count];
            var ranks = new byte[points.Count];

            for (var i = 0; i < parents.Length; i++)
                parents[i] = i;

            var triangles = BuildUvTriangles(
                vertices,
                indices,
                vertexToPoint,
                parents,
                ranks,
                out var existingTriangles);

            if (triangles.Count == 0)
                return 0;

            BuildComponentOrientation(triangles, parents, out var componentSigns);

            var edges = BuildBoundaryEdges(triangles, componentSigns);
            var directedAdjacency = new Dictionary<int, List<int>>();
            var boundaryAdjacency = new Dictionary<int, List<int>>();

            foreach (var edge in edges.Values)
            {
                if (edge.Count != 1)
                    continue;

                AddUnique(directedAdjacency, edge.A, edge.B);

                AddUnique(boundaryAdjacency, edge.A, edge.B);
                AddUnique(boundaryAdjacency, edge.B, edge.A);
            }

            if (directedAdjacency.Count == 0)
                return 0;

            var boundaryComponents = BuildBoundaryComponents(points.Count, boundaryAdjacency);
            var usedBoundaryComponents = new HashSet<int>();
            var added = 0;

            foreach (var item in directedAdjacency)
            {
                var a = item.Key;

                foreach (var b in item.Value)
                {
                    if (!directedAdjacency.TryGetValue(b, out var nextList))
                        continue;

                    foreach (var c in nextList)
                    {
                        if (a == c)
                            continue;

                        var boundaryComponent = boundaryComponents[b];

                        if (boundaryComponent < 0)
                            continue;

                        if (usedBoundaryComponents.Contains(boundaryComponent))
                            continue;

                        var key = new TriangleKey(a, b, c);

                        if (existingTriangles.Contains(key))
                            continue;

                        var component = Find(parents, a);

                        if (!componentSigns.TryGetValue(component, out var componentSign))
                            continue;

                        var signedArea = GetSignedArea(points[a].Uv, points[b].Uv, points[c].Uv);

                        if (signedArea * componentSign >= -UvAreaEpsilon)
                            continue;

                        if (!AcceptTriangle(points, vertices, a, b, c))
                            continue;

                        existingTriangles.Add(key);
                        usedBoundaryComponents.Add(boundaryComponent);

                        EmitTriangle(points, componentSign, vertices, indices, a, b, c);

                        added++;
                    }
                }
            }

            return added;
        }

        private List<UvPoint> BuildUvPoints(VertexData[] vertices, out int[] vertexToPoint)
        {
            var points = new List<UvPoint>(vertices.Length);
            var cells = new Dictionary<CellKey, List<int>>(vertices.Length);
            var cellSize = MathF.Max(_uvMergePixels, 0.5f);
            var mergeSq = _uvMergePixels * _uvMergePixels;

            vertexToPoint = new int[vertices.Length];

            for (var i = 0; i < vertices.Length; i++)
            {
                var pixel = vertices[i].UV * _atlasSize;
                var cell = GetCell(pixel, cellSize);
                var found = -1;

                for (var y = -1; y <= 1 && found < 0; y++)
                {
                    for (var x = -1; x <= 1 && found < 0; x++)
                    {
                        var key = new CellKey(cell.X + x, cell.Y + y);

                        if (!cells.TryGetValue(key, out var list))
                            continue;

                        for (var j = 0; j < list.Count; j++)
                        {
                            var pointIndex = list[j];

                            if (Vector2.DistanceSquared(points[pointIndex].Pixel, pixel) > mergeSq)
                                continue;

                            found = pointIndex;
                            break;
                        }
                    }
                }

                if (found < 0)
                {
                    found = points.Count;

                    points.Add(new UvPoint
                    {
                        Uv = vertices[i].UV,
                        Pixel = pixel,
                        RepresentativeIndex = (uint)i
                    });

                    if (!cells.TryGetValue(cell, out var list))
                    {
                        list = new List<int>();
                        cells[cell] = list;
                    }

                    list.Add(found);
                }

                vertexToPoint[i] = found;
            }

            return points;
        }

        private List<UvTriangle> BuildUvTriangles(
            VertexData[] vertices,
            List<uint> indices,
            int[] vertexToPoint,
            int[] parents,
            byte[] ranks,
            out HashSet<TriangleKey> existingTriangles)
        {
            var result = new List<UvTriangle>(indices.Count / 3);
            existingTriangles = new HashSet<TriangleKey>();

            for (var i = 0; i < indices.Count; i += 3)
            {
                var a = vertexToPoint[(int)indices[i + 0]];
                var b = vertexToPoint[(int)indices[i + 1]];
                var c = vertexToPoint[(int)indices[i + 2]];

                if (a == b || b == c || c == a)
                    continue;

                var key = new TriangleKey(a, b, c);

                if (existingTriangles.Contains(key))
                    continue;

                var signedArea = GetSignedArea(
                    vertices[(int)indices[i + 0]].UV,
                    vertices[(int)indices[i + 1]].UV,
                    vertices[(int)indices[i + 2]].UV);

                if (MathF.Abs(signedArea) <= UvAreaEpsilon)
                    continue;

                existingTriangles.Add(key);

                Union(parents, ranks, a, b);
                Union(parents, ranks, b, c);
                Union(parents, ranks, c, a);

                result.Add(new UvTriangle
                {
                    A = a,
                    B = b,
                    C = c,
                    SignedArea = signedArea
                });
            }

            for (var i = 0; i < result.Count; i++)
            {
                var tri = result[i];
                tri.Component = Find(parents, tri.A);
                result[i] = tri;
            }

            return result;
        }

        private static void BuildComponentOrientation(
            List<UvTriangle> triangles,
            int[] parents,
            out Dictionary<int, float> componentSigns)
        {
            var areaSums = new Dictionary<int, float>();

            for (var i = 0; i < triangles.Count; i++)
            {
                var tri = triangles[i];
                var component = Find(parents, tri.A);

                if (areaSums.TryGetValue(component, out var sum))
                    areaSums[component] = sum + tri.SignedArea;
                else
                    areaSums[component] = tri.SignedArea;
            }

            componentSigns = new Dictionary<int, float>(areaSums.Count);

            foreach (var item in areaSums)
                componentSigns[item.Key] = item.Value >= 0.0f ? 1.0f : -1.0f;
        }

        private Dictionary<EdgeKey, EdgeInfo> BuildBoundaryEdges(
            List<UvTriangle> triangles,
            Dictionary<int, float> componentSigns)
        {
            var edges = new Dictionary<EdgeKey, EdgeInfo>(triangles.Count * 3);

            for (var i = 0; i < triangles.Count; i++)
            {
                var tri = triangles[i];

                var a = tri.A;
                var b = tri.B;
                var c = tri.C;

                var componentSign = componentSigns[tri.Component];

                if (tri.SignedArea * componentSign < 0.0f)
                    (b, c) = (c, b);

                AddEdge(edges, a, b);
                AddEdge(edges, b, c);
                AddEdge(edges, c, a);
            }

            return edges;
        }

        private bool AcceptTriangle(
            List<UvPoint> points,
            VertexData[] vertices,
            int a,
            int b,
            int c)
        {
            var ia = points[a].RepresentativeIndex;
            var ib = points[b].RepresentativeIndex;
            var ic = points[c].RepresentativeIndex;

            var va = vertices[ia];
            var vb = vertices[ib];
            var vc = vertices[ic];

            var area = Vector3.Cross(vb.Pos - va.Pos, vc.Pos - va.Pos).Length() * 0.5f;

            if (area <= MathF.Max(_minTriangleArea, GeometryAreaEpsilon))
                return false;

            if (_maxEdgeLength <= 0.0f)
                return true;

            var maxEdgeSq = _maxEdgeLength * _maxEdgeLength;

            if (Vector3.DistanceSquared(va.Pos, vb.Pos) > maxEdgeSq)
                return false;

            if (Vector3.DistanceSquared(vb.Pos, vc.Pos) > maxEdgeSq)
                return false;

            if (Vector3.DistanceSquared(vc.Pos, va.Pos) > maxEdgeSq)
                return false;

            return true;
        }

        private static void EmitTriangle(
            List<UvPoint> points,
            float componentSign,
            VertexData[] vertices,
            List<uint> indices,
            int a,
            int b,
            int c)
        {
            var ia = points[a].RepresentativeIndex;
            var ib = points[b].RepresentativeIndex;
            var ic = points[c].RepresentativeIndex;

            var va = vertices[ia];
            var vb = vertices[ib];
            var vc = vertices[ic];

            var normal = Vector3.Cross(vb.Pos - va.Pos, vc.Pos - va.Pos);
            var expected = va.Normal + vb.Normal + vc.Normal;

            if (normal.LengthSquared() > 1E-20f && expected.LengthSquared() > 1E-20f)
            {
                if (Vector3.Dot(normal, expected) < 0.0f)
                {
                    indices.Add(ia);
                    indices.Add(ic);
                    indices.Add(ib);
                    return;
                }

                indices.Add(ia);
                indices.Add(ib);
                indices.Add(ic);
                return;
            }

            var signedArea = GetSignedArea(
                vertices[ia].UV,
                vertices[ib].UV,
                vertices[ic].UV);

            if (signedArea * componentSign > 0.0f)
            {
                indices.Add(ia);
                indices.Add(ic);
                indices.Add(ib);
            }
            else
            {
                indices.Add(ia);
                indices.Add(ib);
                indices.Add(ic);
            }
        }

        private static void AddEdge(
            Dictionary<EdgeKey, EdgeInfo> edges,
            int a,
            int b)
        {
            var key = new EdgeKey(a, b);

            if (edges.TryGetValue(key, out var edge))
            {
                edge.Count++;
                edges[key] = edge;
                return;
            }

            edges[key] = new EdgeInfo
            {
                A = a,
                B = b,
                Count = 1
            };
        }

        private static void AddUnique(
            Dictionary<int, List<int>> adjacency,
            int a,
            int b)
        {
            if (!adjacency.TryGetValue(a, out var list))
            {
                list = new List<int>(2);
                adjacency[a] = list;
            }

            for (var i = 0; i < list.Count; i++)
            {
                if (list[i] == b)
                    return;
            }

            list.Add(b);
        }

        private static int[] BuildBoundaryComponents(
            int pointCount,
            Dictionary<int, List<int>> adjacency)
        {
            var result = new int[pointCount];
            Array.Fill(result, -1);

            var stack = new Stack<int>();
            var component = 0;

            foreach (var item in adjacency)
            {
                var start = item.Key;

                if (result[start] >= 0)
                    continue;

                result[start] = component;
                stack.Push(start);

                while (stack.Count > 0)
                {
                    var current = stack.Pop();

                    if (!adjacency.TryGetValue(current, out var list))
                        continue;

                    for (var i = 0; i < list.Count; i++)
                    {
                        var next = list[i];

                        if (result[next] >= 0)
                            continue;

                        result[next] = component;
                        stack.Push(next);
                    }
                }

                component++;
            }

            return result;
        }

        private static int Find(int[] parents, int index)
        {
            var root = index;

            while (parents[root] != root)
                root = parents[root];

            while (parents[index] != index)
            {
                var next = parents[index];
                parents[index] = root;
                index = next;
            }

            return root;
        }

        private static void Union(int[] parents, byte[] ranks, int a, int b)
        {
            var rootA = Find(parents, a);
            var rootB = Find(parents, b);

            if (rootA == rootB)
                return;

            if (ranks[rootB] > ranks[rootA])
                (rootA, rootB) = (rootB, rootA);

            parents[rootB] = rootA;

            if (ranks[rootA] == ranks[rootB])
                ranks[rootA]++;
        }

        private static float GetSignedArea(Vector2 a, Vector2 b, Vector2 c)
        {
            return ((b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X)) * 0.5f;
        }

        private static CellKey GetCell(Vector2 pixel, float cellSize)
        {
            return new CellKey(
                (int)MathF.Floor(pixel.X / cellSize),
                (int)MathF.Floor(pixel.Y / cellSize));
        }
    }
}