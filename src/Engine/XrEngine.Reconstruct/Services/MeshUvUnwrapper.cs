using System;
using System.Collections.Generic;
using System.Numerics;

namespace XrEngine
{
    public sealed class MeshUvUnwrapperParams
    {
        public MeshUvUnwrapperParams()
        {
            AtlasSize = 4096;
            Padding = 6;
            AllowRotate = true;

            MaxAngleDeg = 65.0f;
            PlaneDistance = 0.08f;

            MinIslandTriangles = 150;
            MinIslandArea = 0.02f;
            MergePasses = 10;

            CoplanarMergePasses = 2;
            CoplanarMaxAngleDeg = 25.0f;
            CoplanarPlaneDistance = 0.08f;
        }

        public int AtlasSize { get; set; }

        public int Padding { get; set; }

        public bool AllowRotate { get; set; }

        public float MaxAngleDeg { get; set; }

        public float PlaneDistance { get; set; }

        public int MinIslandTriangles { get; set; }

        public float MinIslandArea { get; set; }

        public int MergePasses { get; set; }

        public int CoplanarMergePasses { get; set; }

        public float CoplanarMaxAngleDeg { get; set; }

        public float CoplanarPlaneDistance { get; set; }
    }

    public sealed class MeshUvUnwrapper
    {
        #region Private Structs

        private readonly struct EdgeKey : IEquatable<EdgeKey>
        {
            public EdgeKey(uint a, uint b)
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
                return HashCode.Combine(A, B);
            }

            public uint A { get; }

            public uint B { get; }
        }

        private readonly struct VertexKey : IEquatable<VertexKey>
        {
            public VertexKey(uint vertexIndex, int chartIndex)
            {
                VertexIndex = vertexIndex;
                ChartIndex = chartIndex;
            }

            public bool Equals(VertexKey other)
            {
                return VertexIndex == other.VertexIndex &&
                       ChartIndex == other.ChartIndex;
            }

            public override bool Equals(object? obj)
            {
                return obj is VertexKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(VertexIndex, ChartIndex);
            }

            public uint VertexIndex { get; }

            public int ChartIndex { get; }
        }

        private readonly struct PackRect
        {
            public PackRect(int x, int y, int width, int height)
            {
                X = x;
                Y = y;
                Width = width;
                Height = height;
            }

            public int X { get; }

            public int Y { get; }

            public int Width { get; }

            public int Height { get; }

            public int Right => X + Width;

            public int Bottom => Y + Height;
        }

        private struct TriangleInfo
        {
            public uint A;
            public uint B;
            public uint C;
            public int Chart;
            public float Area;
            public Vector3 Center;
            public Vector3 Normal;
        }

        private struct PackResult
        {
            public float Scale;
            public bool Success;
        }

        private struct Placement
        {
            public bool HasValue;
            public int X;
            public int Y;
            public int Width;
            public int Height;
            public int ScoreA;
            public int ScoreB;
            public bool Rotated;
        }

        #endregion

        #region Private Classes

        private sealed class Chart
        {
            public Chart()
            {
                Triangles = new List<int>();
                Min = new Vector2(float.MaxValue, float.MaxValue);
                Max = new Vector2(float.MinValue, float.MinValue);
            }

            public void AddTriangle(int triIndex, TriangleInfo tri)
            {
                Triangles.Add(triIndex);

                Area += tri.Area;
                NormalSum += tri.Normal * tri.Area;
                CenterSum += tri.Center * tri.Area;
            }

            public void FinalizePlane()
            {
                if (Area > 0.0f)
                    Center = CenterSum / Area;

                Normal = NormalizeSafe(NormalSum, Vector3.UnitY);

                AxisX = Vector3.Cross(Vector3.UnitY, Normal);

                if (AxisX.LengthSquared() < 0.000001f)
                    AxisX = Vector3.Cross(Vector3.UnitX, Normal);

                AxisX = NormalizeSafe(AxisX, Vector3.UnitX);
                AxisY = NormalizeSafe(Vector3.Cross(Normal, AxisX), Vector3.UnitZ);
            }

            public List<int> Triangles;
            public float Area;
            public Vector3 Center;
            public Vector3 CenterSum;
            public Vector3 Normal;
            public Vector3 NormalSum;
            public Vector3 AxisX;
            public Vector3 AxisY;
            public Vector2 Min;
            public Vector2 Max;
            public int PackedX;
            public int PackedY;
            public int PackedWidth;
            public int PackedHeight;
            public bool Rotated;
        }

        #endregion

        private Geometry3D _geometry = null!;
        private VertexData[] _vertices = Array.Empty<VertexData>();
        private uint[] _indices = Array.Empty<uint>();
        private TriangleInfo[] _triangles = Array.Empty<TriangleInfo>();
        private List<int>[] _adjacency = Array.Empty<List<int>>();

        private int _atlasSize;
        private int _padding;
        private bool _allowRotate;

        private float _maxAngleDeg;
        private float _planeDistance;

        private int _minIslandTriangles;
        private float _minIslandArea;
        private int _mergePasses;

        private int _coplanarMergePasses;
        private float _coplanarMaxAngleDeg;
        private float _coplanarPlaneDistance;

        public MeshUvUnwrapper()
        {
            SetParameters(new MeshUvUnwrapperParams());
        }

        public MeshUvUnwrapper(MeshUvUnwrapperParams parameters)
        {
            SetParameters(parameters);
        }

        public void SetParameters(MeshUvUnwrapperParams parameters)
        {
            _atlasSize = parameters.AtlasSize;
            _padding = parameters.Padding;
            _allowRotate = parameters.AllowRotate;

            _maxAngleDeg = parameters.MaxAngleDeg;
            _planeDistance = parameters.PlaneDistance;

            _minIslandTriangles = parameters.MinIslandTriangles;
            _minIslandArea = parameters.MinIslandArea;
            _mergePasses = parameters.MergePasses;

            _coplanarMergePasses = parameters.CoplanarMergePasses;
            _coplanarMaxAngleDeg = parameters.CoplanarMaxAngleDeg;
            _coplanarPlaneDistance = parameters.CoplanarPlaneDistance;
        }

        public void Unwrap(Geometry3D geometry)
        {
            _geometry = geometry;

            _geometry.EnsureIndices();

            _vertices = _geometry.Vertices;
            _indices = _geometry.Indices;

            BuildTriangles();
            BuildAdjacency();
            BuildInitialCharts();
            MergeSmallCharts();
            MergeCoplanarCharts();

            var charts = BuildCharts();

            BuildChartProjectionBounds(charts);

            var pack = PackCharts(charts);

            if (!pack.Success)
                throw new InvalidOperationException("Unable to pack UV islands.");

            RebuildGeometry(charts, pack.Scale);
        }

        private void BuildTriangles()
        {
            _triangles = new TriangleInfo[_indices.Length / 3];

            for (var tri = 0; tri < _triangles.Length; tri++)
            {
                var i = tri * 3;

                var ia = _indices[i + 0];
                var ib = _indices[i + 1];
                var ic = _indices[i + 2];

                var a = _vertices[ia].Pos;
                var b = _vertices[ib].Pos;
                var c = _vertices[ic].Pos;

                var cross = Vector3.Cross(b - a, c - a);
                var crossLen = cross.Length();

                _triangles[tri] = new TriangleInfo
                {
                    A = ia,
                    B = ib,
                    C = ic,
                    Chart = -1,
                    Area = crossLen * 0.5f,
                    Center = (a + b + c) / 3.0f,
                    Normal = crossLen > 0.000001f ? cross / crossLen : Vector3.UnitY
                };
            }
        }

        private void BuildAdjacency()
        {
            _adjacency = new List<int>[_triangles.Length];

            var edges = new Dictionary<EdgeKey, int>(_triangles.Length * 3);

            for (var i = 0; i < _adjacency.Length; i++)
                _adjacency[i] = new List<int>(3);

            for (var tri = 0; tri < _triangles.Length; tri++)
            {
                var i = tri * 3;

                AddEdge(_indices[i + 0], _indices[i + 1], tri, edges);
                AddEdge(_indices[i + 1], _indices[i + 2], tri, edges);
                AddEdge(_indices[i + 2], _indices[i + 0], tri, edges);
            }
        }

        private void AddEdge(
            uint a,
            uint b,
            int tri,
            Dictionary<EdgeKey, int> edges)
        {
            var key = new EdgeKey(a, b);

            if (edges.TryGetValue(key, out var otherTri))
            {
                _adjacency[tri].Add(otherTri);
                _adjacency[otherTri].Add(tri);
            }
            else
            {
                edges[key] = tri;
            }
        }

        private void BuildInitialCharts()
        {
            var normalDotLimit = MathF.Cos(_maxAngleDeg * MathF.PI / 180.0f);
            var queue = new Queue<int>();
            var chartIndex = 0;

            for (var seed = 0; seed < _triangles.Length; seed++)
            {
                if (_triangles[seed].Chart >= 0)
                    continue;

                var chart = new Chart();

                _triangles[seed].Chart = chartIndex;
                chart.AddTriangle(seed, _triangles[seed]);
                chart.FinalizePlane();

                queue.Enqueue(seed);

                while (queue.Count > 0)
                {
                    var triIndex = queue.Dequeue();

                    foreach (var nextIndex in _adjacency[triIndex])
                    {
                        if (_triangles[nextIndex].Chart >= 0)
                            continue;

                        var next = _triangles[nextIndex];

                        if (!CanJoinChart(next, chart, normalDotLimit, _planeDistance))
                            continue;

                        next.Chart = chartIndex;
                        _triangles[nextIndex] = next;

                        chart.AddTriangle(nextIndex, next);
                        chart.FinalizePlane();

                        queue.Enqueue(nextIndex);
                    }
                }

                chartIndex++;
            }
        }

        private static bool CanJoinChart(
            TriangleInfo triangle,
            Chart chart,
            float normalDotLimit,
            float planeDistanceLimit)
        {
            var normalDot = Vector3.Dot(triangle.Normal, chart.Normal);

            if (normalDot < normalDotLimit)
                return false;

            var planeDistance = MathF.Abs(Vector3.Dot(triangle.Center - chart.Center, chart.Normal));

            return planeDistance <= planeDistanceLimit;
        }

        private void MergeSmallCharts()
        {
            for (var pass = 0; pass < _mergePasses; pass++)
            {
                var charts = BuildCharts();
                var targets = new int[charts.Length];

                Array.Fill(targets, -1);

                var changed = false;

                for (var chartIndex = 0; chartIndex < charts.Length; chartIndex++)
                {
                    var chart = charts[chartIndex];

                    if (chart.Triangles.Count >= _minIslandTriangles &&
                        chart.Area >= _minIslandArea)
                    {
                        continue;
                    }

                    var target = FindBestNeighborChart(chartIndex, chart, charts);

                    if (target < 0)
                        continue;

                    targets[chartIndex] = target;
                    changed = true;
                }

                if (!changed)
                    break;

                ApplyChartTargets(targets);
                CompactChartIds();
            }
        }

        private int FindBestNeighborChart(
            int chartIndex,
            Chart chart,
            Chart[] charts)
        {
            var bestChart = -1;
            var bestScore = float.MinValue;

            foreach (var triIndex in chart.Triangles)
            {
                foreach (var neighborTri in _adjacency[triIndex])
                {
                    var neighborChart = _triangles[neighborTri].Chart;

                    if (neighborChart == chartIndex)
                        continue;

                    var other = charts[neighborChart];

                    if (other.Area <= chart.Area)
                        continue;

                    var score = Vector3.Dot(chart.Normal, other.Normal);

                    if (score <= bestScore)
                        continue;

                    bestScore = score;
                    bestChart = neighborChart;
                }
            }

            return bestChart;
        }

        private void MergeCoplanarCharts()
        {
            var normalDotLimit = MathF.Cos(_coplanarMaxAngleDeg * MathF.PI / 180.0f);

            for (var pass = 0; pass < _coplanarMergePasses; pass++)
            {
                var charts = BuildCharts();
                var targets = new int[charts.Length];

                Array.Fill(targets, -1);

                var changed = false;

                for (var chartIndex = 0; chartIndex < charts.Length; chartIndex++)
                {
                    var source = charts[chartIndex];
                    var target = FindBestCoplanarChart(chartIndex, source, charts, normalDotLimit);

                    if (target < 0)
                        continue;

                    targets[chartIndex] = target;
                    changed = true;
                }

                if (!changed)
                    break;

                ApplyChartTargets(targets);
                CompactChartIds();
            }
        }

        private int FindBestCoplanarChart(
            int chartIndex,
            Chart source,
            Chart[] charts,
            float normalDotLimit)
        {
            var bestChart = -1;
            var bestScore = float.MinValue;

            for (var otherIndex = 0; otherIndex < charts.Length; otherIndex++)
            {
                if (otherIndex == chartIndex)
                    continue;

                var other = charts[otherIndex];

                if (other.Area <= source.Area)
                    continue;

                var normalDot = Vector3.Dot(source.Normal, other.Normal);

                if (normalDot < normalDotLimit)
                    continue;

                var distance0 = MathF.Abs(Vector3.Dot(source.Center - other.Center, source.Normal));
                var distance1 = MathF.Abs(Vector3.Dot(other.Center - source.Center, other.Normal));

                if (distance0 > _coplanarPlaneDistance ||
                    distance1 > _coplanarPlaneDistance)
                {
                    continue;
                }

                var score = normalDot - (distance0 + distance1);

                if (score <= bestScore)
                    continue;

                bestScore = score;
                bestChart = otherIndex;
            }

            return bestChart;
        }

        private void ApplyChartTargets(int[] targets)
        {
            for (var triIndex = 0; triIndex < _triangles.Length; triIndex++)
            {
                var tri = _triangles[triIndex];
                var target = ResolveTarget(tri.Chart, targets);

                if (target < 0)
                    continue;

                tri.Chart = target;
                _triangles[triIndex] = tri;
            }
        }

        private static int ResolveTarget(int chart, int[] targets)
        {
            var target = targets[chart];

            while (target >= 0 && targets[target] >= 0)
                target = targets[target];

            return target;
        }

        private void CompactChartIds()
        {
            var map = new Dictionary<int, int>();

            for (var i = 0; i < _triangles.Length; i++)
            {
                var chart = _triangles[i].Chart;

                if (!map.TryGetValue(chart, out var newChart))
                {
                    newChart = map.Count;
                    map[chart] = newChart;
                }

                _triangles[i].Chart = newChart;
            }
        }

        private Chart[] BuildCharts()
        {
            var chartCount = 0;

            for (var i = 0; i < _triangles.Length; i++)
                chartCount = Math.Max(chartCount, _triangles[i].Chart + 1);

            var charts = new Chart[chartCount];

            for (var i = 0; i < charts.Length; i++)
                charts[i] = new Chart();

            for (var tri = 0; tri < _triangles.Length; tri++)
                charts[_triangles[tri].Chart].AddTriangle(tri, _triangles[tri]);

            for (var i = 0; i < charts.Length; i++)
                charts[i].FinalizePlane();

            return charts;
        }

        private void BuildChartProjectionBounds(Chart[] charts)
        {
            for (var chartIndex = 0; chartIndex < charts.Length; chartIndex++)
            {
                var chart = charts[chartIndex];
                var usedVertices = new HashSet<uint>();

                foreach (var triIndex in chart.Triangles)
                {
                    var tri = _triangles[triIndex];

                    usedVertices.Add(tri.A);
                    usedVertices.Add(tri.B);
                    usedVertices.Add(tri.C);
                }

                foreach (var vertexIndex in usedVertices)
                {
                    var uv = ProjectToChart(_vertices[vertexIndex].Pos, chart);

                    chart.Min = Vector2.Min(chart.Min, uv);
                    chart.Max = Vector2.Max(chart.Max, uv);
                }
            }
        }

        private PackResult PackCharts(Chart[] charts)
        {
            var high = ComputeInitialScale(charts);
            var low = 0.0f;
            var best = new PackResult();

            for (var i = 0; i < 24; i++)
            {
                var mid = (low + high) * 0.5f;

                if (TryPackCharts(charts, mid))
                {
                    best.Scale = mid;
                    best.Success = true;
                    low = mid;
                }
                else
                {
                    high = mid;
                }
            }

            if (best.Success)
                TryPackCharts(charts, best.Scale);

            return best;
        }

        private float ComputeInitialScale(Chart[] charts)
        {
            var maxSize = 0.0001f;

            foreach (var chart in charts)
            {
                var size = chart.Max - chart.Min;
                maxSize = MathF.Max(maxSize, MathF.Max(size.X, size.Y));
            }

            return (_atlasSize - _padding * 2) / maxSize;
        }

        private bool TryPackCharts(Chart[] charts, float scale)
        {
            var freeRects = new List<PackRect>
            {
                new PackRect(0, 0, _atlasSize, _atlasSize)
            };

            var order = BuildPackOrder(charts, scale);

            foreach (var chartIndex in order)
            {
                var chart = charts[chartIndex];
                var placement = FindPlacement(chart, scale, freeRects);

                if (!placement.HasValue)
                    return false;

                chart.PackedX = placement.X;
                chart.PackedY = placement.Y;
                chart.PackedWidth = placement.Width;
                chart.PackedHeight = placement.Height;
                chart.Rotated = placement.Rotated;

                SplitFreeRects(
                    freeRects,
                    new PackRect(
                        placement.X,
                        placement.Y,
                        placement.Width,
                        placement.Height));

                PruneFreeRects(freeRects);
            }

            return true;
        }

        private int[] BuildPackOrder(Chart[] charts, float scale)
        {
            var order = new int[charts.Length];

            for (var i = 0; i < order.Length; i++)
                order[i] = i;

            Array.Sort(
                order,
                (a, b) =>
                {
                    GetChartPackedSize(charts[a], scale, false, out var aw, out var ah);
                    GetChartPackedSize(charts[b], scale, false, out var bw, out var bh);

                    var areaA = aw * ah;
                    var areaB = bw * bh;

                    if (areaA != areaB)
                        return areaB.CompareTo(areaA);

                    var maxA = Math.Max(aw, ah);
                    var maxB = Math.Max(bw, bh);

                    return maxB.CompareTo(maxA);
                });

            return order;
        }

        private Placement FindPlacement(
            Chart chart,
            float scale,
            List<PackRect> freeRects)
        {
            var best = new Placement
            {
                HasValue = false,
                ScoreA = int.MaxValue,
                ScoreB = int.MaxValue
            };

            TryFindPlacement(chart, scale, false, freeRects, ref best);

            if (_allowRotate)
                TryFindPlacement(chart, scale, true, freeRects, ref best);

            return best;
        }

        private void TryFindPlacement(
            Chart chart,
            float scale,
            bool rotated,
            List<PackRect> freeRects,
            ref Placement best)
        {
            GetChartPackedSize(chart, scale, rotated, out var width, out var height);

            if (width > _atlasSize || height > _atlasSize)
                return;

            foreach (var freeRect in freeRects)
            {
                if (width > freeRect.Width || height > freeRect.Height)
                    continue;

                var leftoverX = freeRect.Width - width;
                var leftoverY = freeRect.Height - height;

                var scoreA = Math.Min(leftoverX, leftoverY);
                var scoreB = Math.Max(leftoverX, leftoverY);

                if (scoreA > best.ScoreA)
                    continue;

                if (scoreA == best.ScoreA && scoreB >= best.ScoreB)
                    continue;

                best.HasValue = true;
                best.X = freeRect.X;
                best.Y = freeRect.Y;
                best.Width = width;
                best.Height = height;
                best.ScoreA = scoreA;
                best.ScoreB = scoreB;
                best.Rotated = rotated;
            }
        }

        private void GetChartPackedSize(
            Chart chart,
            float scale,
            bool rotated,
            out int width,
            out int height)
        {
            var size = chart.Max - chart.Min;

            var sx = MathF.Max(size.X, 0.000001f);
            var sy = MathF.Max(size.Y, 0.000001f);

            if (rotated)
                (sx, sy) = (sy, sx);

            width = Math.Max(
                1,
                (int)MathF.Ceiling(sx * scale) + _padding * 2);

            height = Math.Max(
                1,
                (int)MathF.Ceiling(sy * scale) + _padding * 2);
        }

        private static void SplitFreeRects(
            List<PackRect> freeRects,
            PackRect used)
        {
            for (var i = freeRects.Count - 1; i >= 0; i--)
            {
                var free = freeRects[i];

                if (!Intersects(free, used))
                    continue;

                freeRects.RemoveAt(i);

                if (used.X > free.X)
                {
                    freeRects.Add(new PackRect(
                        free.X,
                        free.Y,
                        used.X - free.X,
                        free.Height));
                }

                if (used.Right < free.Right)
                {
                    freeRects.Add(new PackRect(
                        used.Right,
                        free.Y,
                        free.Right - used.Right,
                        free.Height));
                }

                if (used.Y > free.Y)
                {
                    freeRects.Add(new PackRect(
                        free.X,
                        free.Y,
                        free.Width,
                        used.Y - free.Y));
                }

                if (used.Bottom < free.Bottom)
                {
                    freeRects.Add(new PackRect(
                        free.X,
                        used.Bottom,
                        free.Width,
                        free.Bottom - used.Bottom));
                }
            }
        }

        private static bool Intersects(PackRect a, PackRect b)
        {
            return a.X < b.Right &&
                   a.Right > b.X &&
                   a.Y < b.Bottom &&
                   a.Bottom > b.Y;
        }

        private static bool Contains(PackRect outer, PackRect inner)
        {
            return inner.X >= outer.X &&
                   inner.Y >= outer.Y &&
                   inner.Right <= outer.Right &&
                   inner.Bottom <= outer.Bottom;
        }

        private static void PruneFreeRects(List<PackRect> freeRects)
        {
            for (var i = 0; i < freeRects.Count; i++)
            {
                var a = freeRects[i];

                if (a.Width <= 0 || a.Height <= 0)
                {
                    freeRects.RemoveAt(i);
                    i--;
                    continue;
                }

                for (var j = i + 1; j < freeRects.Count; j++)
                {
                    var b = freeRects[j];

                    if (Contains(a, b))
                    {
                        freeRects.RemoveAt(j);
                        j--;
                        continue;
                    }

                    if (Contains(b, a))
                    {
                        freeRects.RemoveAt(i);
                        i--;
                        break;
                    }
                }
            }
        }

        private void RebuildGeometry(Chart[] charts, float scale)
        {
            var newVertices = new List<VertexData>(_vertices.Length);
            var newIndices = new List<uint>(_indices.Length);
            var vertexMap = new Dictionary<VertexKey, uint>();

            for (var triIndex = 0; triIndex < _triangles.Length; triIndex++)
            {
                var tri = _triangles[triIndex];
                var chart = charts[tri.Chart];

                newIndices.Add(GetOrCreateVertex(tri.A, tri.Chart, chart, newVertices, vertexMap, scale));
                newIndices.Add(GetOrCreateVertex(tri.B, tri.Chart, chart, newVertices, vertexMap, scale));
                newIndices.Add(GetOrCreateVertex(tri.C, tri.Chart, chart, newVertices, vertexMap, scale));
            }

            _geometry.Vertices = newVertices.ToArray();
            _geometry.Indices = newIndices.ToArray();
        }

        private uint GetOrCreateVertex(
            uint oldIndex,
            int chartIndex,
            Chart chart,
            List<VertexData> newVertices,
            Dictionary<VertexKey, uint> vertexMap,
            float scale)
        {
            var key = new VertexKey(oldIndex, chartIndex);

            if (vertexMap.TryGetValue(key, out var newIndex))
                return newIndex;

            var vertex = _vertices[oldIndex];

            var localUv = ProjectToChart(vertex.Pos, chart) - chart.Min;
            var chartSize = chart.Max - chart.Min;

            if (chart.Rotated)
            {
                localUv = new Vector2(
                    localUv.Y,
                    chartSize.X - localUv.X);
            }

            var pixelUv = new Vector2(
                chart.PackedX + _padding + localUv.X * scale,
                chart.PackedY + _padding + localUv.Y * scale);

            vertex.UV = pixelUv / _atlasSize;

            newIndex = (uint)newVertices.Count;
            newVertices.Add(vertex);
            vertexMap[key] = newIndex;

            return newIndex;
        }

        private static Vector2 ProjectToChart(Vector3 pos, Chart chart)
        {
            return new Vector2(
                Vector3.Dot(pos, chart.AxisX),
                Vector3.Dot(pos, chart.AxisY));
        }

        private static Vector3 NormalizeSafe(Vector3 value, Vector3 fallback)
        {
            var lenSq = value.LengthSquared();

            if (lenSq < 0.000001f)
                return fallback;

            return value / MathF.Sqrt(lenSq);
        }


    }
}