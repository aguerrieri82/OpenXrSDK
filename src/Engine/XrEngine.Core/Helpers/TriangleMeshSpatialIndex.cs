using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Numerics;
using XrMath;

namespace XrEngine
{
    public sealed class TriangleMeshSpatialIndex
    {
        #region Public Structs


        public readonly struct TriangleSearchHit
        {
            public TriangleSearchHit(Triangle3 triangle, float centerDistanceSq)
            {
                Triangle = triangle;
                CenterDistanceSq = centerDistanceSq;
            }

            public readonly Triangle3 Triangle;

            public readonly float CenterDistanceSq;

            public float CenterDistance => MathF.Sqrt(CenterDistanceSq);
        }

        #endregion

        private const float NormalLengthSqEpsilon = 1E-20f;

        private readonly Geometry3D _geometry;

        private VertexData[] _vertices;
        private uint[] _indices;
        private Triangle3[] _triangles;
        private Vector3I[][] _triangleCells;
        private Dictionary<Vector3I, List<int>> _cells;
        private int[] _visitStamp;
        private int _stamp;
        private float _cellSize;
        private long _lastVersion;

        public TriangleMeshSpatialIndex(Geometry3D geometry, float cellSize = 0.10f)
        {
            _geometry = geometry;
            _vertices = Array.Empty<VertexData>();
            _indices = Array.Empty<uint>();
            _triangles = Array.Empty<Triangle3>();
            _triangleCells = Array.Empty<Vector3I[]>();
            _cells = new Dictionary<Vector3I, List<int>>();
            _visitStamp = Array.Empty<int>();
            _cellSize = cellSize;
            _lastVersion = -1;

            Rebuild(cellSize);
        }

        public void Rebuild(float? cellSize = null)
        {
            if (cellSize.HasValue)
            {
                if (cellSize.Value <= 0.0f)
                    throw new ArgumentOutOfRangeException(nameof(cellSize));

                _cellSize = cellSize.Value;
            }

            _geometry.EnsureIndices();

            _vertices = _geometry.Vertices;
            _indices = _geometry.Indices;

            var triCount = _indices.Length / 3;

            _triangles = new Triangle3[triCount];
            _triangleCells = new Vector3I[triCount][];
            _visitStamp = new int[triCount];
            _cells = new Dictionary<Vector3I, List<int>>(triCount);
            _stamp = 0;

            for (var tri = 0; tri < triCount; tri++)
            {
                var item = BuildTriangle(tri);

                _triangles[tri] = item;
                _triangleCells[tri] = AddTriangleToCells(tri, item.Min, item.Max);
            }

            _lastVersion = _geometry.Version;
        }

        public void Update()
        {
            Rebuild(_cellSize);
        }

        public void EnsureUpdated()
        {
            if (_lastVersion != _geometry.Version)
                Rebuild();
        }

        public void UpdateTriangle(int triangleId)
        {
            if ((uint)triangleId >= (uint)_triangles.Length)
                throw new ArgumentOutOfRangeException(nameof(triangleId));

            _vertices = _geometry.Vertices;
            _indices = _geometry.Indices;

            if (_indices.Length / 3 != _triangles.Length)
            {
                Rebuild();
                return;
            }

            RemoveTriangleFromCells(triangleId);

            var item = BuildTriangle(triangleId);

            _triangles[triangleId] = item;
            _triangleCells[triangleId] = AddTriangleToCells(triangleId, item.Min, item.Max);
            _lastVersion = _geometry.Version;
        }

        public void UpdateTriangles(IReadOnlyList<int> triangleIds)
        {
            for (var i = 0; i < triangleIds.Count; i++)
                UpdateTriangle(triangleIds[i]);
        }

        public Triangle3 GetTriangle(int triangleId)
        {
            EnsureUpdated();

            if ((uint)triangleId >= (uint)_triangles.Length)
                throw new ArgumentOutOfRangeException(nameof(triangleId));

            return _triangles[triangleId];
        }

        public bool TryGetTriangle(int triangleId, out Triangle3 triangle)
        {
            EnsureUpdated();

            if ((uint)triangleId >= (uint)_triangles.Length)
            {
                triangle = default;
                return false;
            }

            triangle = _triangles[triangleId];
            return true;
        }

        public List<TriangleSearchHit> SearchBounds(
            Vector3 min,
            Vector3 max,
            List<TriangleSearchHit>? result = null,
            Vector3? referenceCenter = null,
            bool sortByDistance = false)
        {
            result ??= new List<TriangleSearchHit>(256);
            result.Clear();

            SearchBoundsInternal(
                min,
                max,
                referenceCenter ?? ((min + max) * 0.5f),
                null,
                result);

            if (sortByDistance)
                result.Sort((a, b) => a.CenterDistanceSq.CompareTo(b.CenterDistanceSq));

            return result;
        }

        public List<TriangleSearchHit> SearchSphere(
            Vector3 center,
            float radius,
            List<TriangleSearchHit>? result = null,
            bool sortByDistance = false)
        {
            result ??= new List<TriangleSearchHit>(256);
            result.Clear();

            var ext = new Vector3(radius);
            var radiusSq = radius * radius;

            SearchBoundsInternal(center - ext, center + ext, center, null, result);

            for (var i = result.Count - 1; i >= 0; i--)
            {
                var tri = result[i].Triangle;

                if (!IntersectsSphere(tri.Min, tri.Max, center, radiusSq))
                    result.RemoveAt(i);
            }

            if (sortByDistance)
                result.Sort((a, b) => a.CenterDistanceSq.CompareTo(b.CenterDistanceSq));

            return result;
        }

        public List<TriangleSearchHit> SearchAroundTriangle(
            int triangleId,
            float padding = 0.05f,
            bool includeSelf = false,
            List<TriangleSearchHit>? result = null,
            bool sortByDistance = true)
        {
            EnsureUpdated();

            if ((uint)triangleId >= (uint)_triangles.Length)
                throw new ArgumentOutOfRangeException(nameof(triangleId));

            result ??= new List<TriangleSearchHit>(256);
            result.Clear();

            var tri = _triangles[triangleId];
            var ext = new Vector3(padding);
            var excluded = includeSelf ? null : new HashSet<int> { triangleId };

            SearchBoundsInternal(
                tri.Min - ext,
                tri.Max + ext,
                tri.Center,
                excluded,
                result);

            if (sortByDistance)
                result.Sort((a, b) => a.CenterDistanceSq.CompareTo(b.CenterDistanceSq));

            return result;
        }

        public List<TriangleSearchHit> SearchAroundTriangles(
            IReadOnlyList<int> triangleIds,
            float padding = 0.05f,
            bool includeSelected = false,
            List<TriangleSearchHit>? result = null,
            bool sortByDistance = true)
        {
            EnsureUpdated();

            result ??= new List<TriangleSearchHit>(256);
            result.Clear();

            if (triangleIds.Count == 0)
                return result;

            var min = new Vector3(float.PositiveInfinity);
            var max = new Vector3(float.NegativeInfinity);
            var center = Vector3.Zero;
            var selected = includeSelected ? null : new HashSet<int>(triangleIds.Count);
            var count = 0;

            for (var i = 0; i < triangleIds.Count; i++)
            {
                var triangleId = triangleIds[i];

                if ((uint)triangleId >= (uint)_triangles.Length)
                    continue;

                var tri = _triangles[triangleId];

                min = Vector3.Min(min, tri.Min);
                max = Vector3.Max(max, tri.Max);
                center += tri.Center;
                count++;

                selected?.Add(triangleId);
            }

            if (count == 0)
                return result;

            center /= count;

            var ext = new Vector3(padding);

            SearchBoundsInternal(
                min - ext,
                max + ext,
                center,
                selected,
                result);

            if (sortByDistance)
                result.Sort((a, b) => a.CenterDistanceSq.CompareTo(b.CenterDistanceSq));

            return result;
        }

        public bool TryFindNearestTriangleCenter(
            Vector3 point,
            float radius,
            out TriangleSearchHit hit)
        {
            var hits = SearchSphere(point, radius, null, false);

            if (hits.Count == 0)
            {
                hit = default;
                return false;
            }

            var best = hits[0];

            for (var i = 1; i < hits.Count; i++)
            {
                if (hits[i].CenterDistanceSq < best.CenterDistanceSq)
                    best = hits[i];
            }

            hit = best;
            return true;
        }

        public void ForEachBounds(
            Vector3 min,
            Vector3 max,
            Action<int> callback,
            bool preciseBounds = true)
        {
            EnsureUpdated();
            BeginVisit();

            var minCell = GetCell(min);
            var maxCell = GetCell(max);

            for (var z = minCell.Z; z <= maxCell.Z; z++)
            {
                for (var y = minCell.Y; y <= maxCell.Y; y++)
                {
                    for (var x = minCell.X; x <= maxCell.X; x++)
                    {
                        var key = new Vector3I(x, y, z);

                        if (!_cells.TryGetValue(key, out var list))
                            continue;

                        for (var i = 0; i < list.Count; i++)
                        {
                            var triangleId = list[i];

                            if (_visitStamp[triangleId] == _stamp)
                                continue;

                            _visitStamp[triangleId] = _stamp;

                            if (preciseBounds)
                            {
                                var tri = _triangles[triangleId];

                                if (!Intersects(tri.Min, tri.Max, min, max))
                                    continue;
                            }

                            callback(triangleId);
                        }
                    }
                }
            }
        }

        public void ForEachTriangleBounds(
            Vector3 min,
            Vector3 max,
            Action<Triangle3> callback,
            bool preciseBounds = true)
        {
            EnsureUpdated();
            BeginVisit();

            var minCell = GetCell(min);
            var maxCell = GetCell(max);

            for (var z = minCell.Z; z <= maxCell.Z; z++)
            {
                for (var y = minCell.Y; y <= maxCell.Y; y++)
                {
                    for (var x = minCell.X; x <= maxCell.X; x++)
                    {
                        var key = new Vector3I(x, y, z);

                        if (!_cells.TryGetValue(key, out var list))
                            continue;

                        for (var i = 0; i < list.Count; i++)
                        {
                            var triangleId = list[i];

                            if (_visitStamp[triangleId] == _stamp)
                                continue;

                            _visitStamp[triangleId] = _stamp;

                            var tri = _triangles[triangleId];

                            if (preciseBounds && !Intersects(tri.Min, tri.Max, min, max))
                                continue;

                            callback(tri);
                        }
                    }
                }
            }
        }

        public void Clear()
        {
            _vertices = Array.Empty<VertexData>();
            _indices = Array.Empty<uint>();
            _triangles = Array.Empty<Triangle3>();
            _triangleCells = Array.Empty<Vector3I[]>();
            _cells.Clear();
            _visitStamp = Array.Empty<int>();
            _stamp = 0;
            _lastVersion = -1;
        }

        private void SearchBoundsInternal(
            Vector3 min,
            Vector3 max,
            Vector3 referenceCenter,
            HashSet<int>? excluded,
            List<TriangleSearchHit> result)
        {
            EnsureUpdated();
            BeginVisit();

            var minCell = GetCell(min);
            var maxCell = GetCell(max);

            for (var z = minCell.Z; z <= maxCell.Z; z++)
            {
                for (var y = minCell.Y; y <= maxCell.Y; y++)
                {
                    for (var x = minCell.X; x <= maxCell.X; x++)
                    {
                        var key = new Vector3I(x, y, z);

                        if (!_cells.TryGetValue(key, out var list))
                            continue;

                        for (var i = 0; i < list.Count; i++)
                        {
                            var triangleId = list[i];

                            if (_visitStamp[triangleId] == _stamp)
                                continue;

                            _visitStamp[triangleId] = _stamp;

                            if (excluded != null && excluded.Contains(triangleId))
                                continue;

                            var tri = _triangles[triangleId];

                            if (!Intersects(tri.Min, tri.Max, min, max))
                                continue;

                            result.Add(new TriangleSearchHit(
                                tri,
                                Vector3.DistanceSquared(referenceCenter, tri.Center)));
                        }
                    }
                }
            }
        }

        private Triangle3 BuildTriangle(int triangleId)
        {
            var i = triangleId * 3;

            var res = new Triangle3()
            {
                Id = triangleId,
                I0 = _indices[i + 0],
                I1 = _indices[i + 1],
                I2 = _indices[i + 2],

            };
            res.V0 = _vertices[res.I0].Pos;
            res.V1 = _vertices[res.I1].Pos;
            res.V2 = _vertices[res.I2].Pos;

            return res;
        }

        private Vector3I[] AddTriangleToCells(int triangleId, Vector3 min, Vector3 max)
        {
            var minCell = GetCell(min);
            var maxCell = GetCell(max);
            var keys = new List<Vector3I>(8);

            for (var z = minCell.Z; z <= maxCell.Z; z++)
            {
                for (var y = minCell.Y; y <= maxCell.Y; y++)
                {
                    for (var x = minCell.X; x <= maxCell.X; x++)
                    {
                        var key = new Vector3I(x, y, z);

                        if (!_cells.TryGetValue(key, out var list))
                        {
                            list = new List<int>(8);
                            _cells[key] = list;
                        }

                        list.Add(triangleId);
                        keys.Add(key);
                    }
                }
            }

            return keys.ToArray();
        }

        private void RemoveTriangleFromCells(int triangleId)
        {
            var keys = _triangleCells[triangleId];

            for (var i = 0; i < keys.Length; i++)
            {
                var key = keys[i];

                if (!_cells.TryGetValue(key, out var list))
                    continue;

                for (var j = list.Count - 1; j >= 0; j--)
                {
                    if (list[j] == triangleId)
                    {
                        list.RemoveAt(j);
                        break;
                    }
                }

                if (list.Count == 0)
                    _cells.Remove(key);
            }

            _triangleCells[triangleId] = Array.Empty<Vector3I>();
        }

        private void BeginVisit()
        {
            _stamp++;

            if (_stamp == int.MaxValue)
            {
                Array.Clear(_visitStamp);
                _stamp = 1;
            }
        }

        private Vector3I GetCell(Vector3 pos)
        {
            return new Vector3I(
                (int)MathF.Floor(pos.X / _cellSize),
                (int)MathF.Floor(pos.Y / _cellSize),
                (int)MathF.Floor(pos.Z / _cellSize));
        }

        private static bool Intersects(Vector3 minA, Vector3 maxA, Vector3 minB, Vector3 maxB)
        {
            return
                minA.X <= maxB.X && maxA.X >= minB.X &&
                minA.Y <= maxB.Y && maxA.Y >= minB.Y &&
                minA.Z <= maxB.Z && maxA.Z >= minB.Z;
        }

        private static bool IntersectsSphere(Vector3 min, Vector3 max, Vector3 center, float radiusSq)
        {
            var distSq = 0.0f;

            if (center.X < min.X)
                distSq += (min.X - center.X) * (min.X - center.X);
            else if (center.X > max.X)
                distSq += (center.X - max.X) * (center.X - max.X);

            if (center.Y < min.Y)
                distSq += (min.Y - center.Y) * (min.Y - center.Y);
            else if (center.Y > max.Y)
                distSq += (center.Y - max.Y) * (center.Y - max.Y);

            if (center.Z < min.Z)
                distSq += (min.Z - center.Z) * (min.Z - center.Z);
            else if (center.Z > max.Z)
                distSq += (center.Z - max.Z) * (center.Z - max.Z);

            return distSq <= radiusSq;
        }

        public Geometry3D Geometry => _geometry;

        public int TriangleCount => _triangles.Length;

        public int VertexCount => _vertices.Length;

        public float CellSize => _cellSize;
    }
}