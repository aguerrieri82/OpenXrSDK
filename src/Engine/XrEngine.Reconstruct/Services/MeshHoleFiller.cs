using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using XrMath;


namespace XrEngine.Reconstruct
{
    public enum MeshVisualTriangleHoleFillCoordMode
    {
        Position,
        Uv
    }

    public enum MeshVisualTriangleHoleFillEdgeMode
    {
        ThreeEdges,
        TwoEdges,
        ThreeThenTwoEdges
    }

    public readonly struct AddedTriangle
    {
        public AddedTriangle(uint a, uint b, uint c, int existingEdgeCount)
        {
            A = a;
            B = b;
            C = c;
            ExistingEdgeCount = existingEdgeCount;
        }

        public readonly uint A;
        public readonly uint B;
        public readonly uint C;
        public readonly int ExistingEdgeCount;
    }

    public sealed class MeshHoleFillerParams
    {
        public MeshHoleFillerParams()
        {
            CoordMode = MeshVisualTriangleHoleFillCoordMode.Position;
            EdgeMode = MeshVisualTriangleHoleFillEdgeMode.ThreeThenTwoEdges;

            AtlasSize = 4096;

            MaxPasses = 2;
            MaxAddedTriangles = 0;

            MinGeometryArea = 1e-10f;

            MaxGeometryEdgeLength = 0.0f;

            MaxEdgeFactor = 3.0f;
            TwoEdgeMaxEdgeFactor = 2.5f;

            MinNormalDot = 0.0f;
            FixWinding = true;

            RejectCoveredArea = true;
            MaxCoveredAreaRatio = 0.80f;

            //RejectInsideVertices = true;
            //RejectCoveredCenter = true;
            //RejectEdgeIntersections = true;

            //BarycentricEpsilon = 1e-4f;
            //EdgeInteriorEpsilon = 1e-4f;
            //EdgeBoundaryEpsilon = 4e-4f;

            //PlaneTolerance = 0.0f;
            //PlaneToleranceFactor = 0.025f;

            //QueryPadding = 0.01f;
        }

        public MeshVisualTriangleHoleFillCoordMode CoordMode { get; set; }

        public MeshVisualTriangleHoleFillEdgeMode EdgeMode { get; set; }

        public int AtlasSize { get; set; }

        public int MaxPasses { get; set; }

        public int MaxAddedTriangles { get; set; }

        //public float MinCoordArea { get; set; }

        public float MinGeometryArea { get; set; }

        //public float MaxCoordEdgeLength { get; set; }

        public float MaxGeometryEdgeLength { get; set; }

        public float MaxEdgeFactor { get; set; }

        public float TwoEdgeMaxEdgeFactor { get; set; }

        public float MinNormalDot { get; set; }

        public bool FixWinding { get; set; }

        public bool RejectCoveredArea { get; set; }

        public float MaxCoveredAreaRatio { get; set; }


       // public float SpatialCellSize { get; set; }

        //public float SpatialCellFactor { get; set; }

        //public bool RejectInsideVertices { get; set; }

        //public bool RejectCoveredCenter { get; set; }

        //public bool RejectEdgeIntersections { get; set; }

        //public float BarycentricEpsilon { get; set; }

        //public float EdgeInteriorEpsilon { get; set; }

        //public float EdgeBoundaryEpsilon { get; set; }

        //public float PlaneTolerance { get; set; }

        //public float PlaneToleranceFactor { get; set; }

        //public float QueryPadding { get; set; }
    }

    public sealed class MeshHoleFiller
    {
        #region Private Structs

        private readonly struct Edge
        {
            public Edge(int a, int b, Vector3 p0, Vector3 p1)
            {
                A = a;
                B = b;
                P0 = p0;
                P1 = p1;
            }

            public readonly int A;
            public readonly int B;
            public readonly Vector3 P0;
            public readonly Vector3 P1;
        }

        private readonly struct Triangle
        {
            public Triangle(int a, int b, int c, Vector3[] coords)
            {
                A = a;
                B = b;
                C = c;

                P0 = coords[a];
                P1 = coords[b];
                P2 = coords[c];

                Min = Vector3.Min(P0, Vector3.Min(P1, P2));
                Max = Vector3.Max(P0, Vector3.Max(P1, P2));
            }

            public Edge GetEdge(int index)
            {
                if (index == 0)
                    return new Edge(A, B, P0, P1);

                if (index == 1)
                    return new Edge(B, C, P1, P2);

                return new Edge(C, A, P2, P0);
            }

            public Vector3 Center => (P0 + P1 + P2) * (1.0f / 3.0f);

            public readonly int A;
            public readonly int B;
            public readonly int C;

            public readonly Vector3 P0;
            public readonly Vector3 P1;
            public readonly Vector3 P2;

            public readonly Vector3 Min;
            public readonly Vector3 Max;
        }

        private readonly struct Candidate
        {
            public Candidate(int a, int b, int c, int support, Vector3[] coords, VertexData[] vertices)
            {
                A = a;
                B = b;
                C = c;
                Support = support;

                Coord0 = coords[a];
                Coord1 = coords[b];
                Coord2 = coords[c];

                Pos0 = vertices[a].Pos;
                Pos1 = vertices[b].Pos;
                Pos2 = vertices[c].Pos;

                Coord01 = Coord1 - Coord0;
                Coord02 = Coord2 - Coord0;
                Coord12 = Coord2 - Coord1;

                CoordCross = Vector3.Cross(Coord01, Coord02);
                CoordCrossSq = CoordCross.LengthSquared();

                CoordEdge01Sq = Coord01.LengthSquared();
                CoordEdge12Sq = Coord12.LengthSquared();
                CoordEdge02Sq = Coord02.LengthSquared();

                CoordMaxEdgeSq = MathF.Max(CoordEdge01Sq, MathF.Max(CoordEdge12Sq, CoordEdge02Sq));

                Pos01 = Pos1 - Pos0;
                Pos02 = Pos2 - Pos0;
                Pos12 = Pos2 - Pos1;

                GeometryCross = Vector3.Cross(Pos01, Pos02);
                GeometryCrossSq = GeometryCross.LengthSquared();

                GeometryEdge01Sq = Pos01.LengthSquared();
                GeometryEdge12Sq = Pos12.LengthSquared();
                GeometryEdge02Sq = Pos02.LengthSquared();

                GeometryMaxEdgeSq = MathF.Max(GeometryEdge01Sq, MathF.Max(GeometryEdge12Sq, GeometryEdge02Sq));
            }

            public Vector3 CoordCenter => (Coord0 + Coord1 + Coord2) * (1.0f / 3.0f);

            public Vector3 CoordMin => Vector3.Min(Coord0, Vector3.Min(Coord1, Coord2));

            public Vector3 CoordMax => Vector3.Max(Coord0, Vector3.Max(Coord1, Coord2));

            public readonly int A;
            public readonly int B;
            public readonly int C;
            public readonly int Support;

            public readonly Vector3 Coord0;
            public readonly Vector3 Coord1;
            public readonly Vector3 Coord2;

            public readonly Vector3 Pos0;
            public readonly Vector3 Pos1;
            public readonly Vector3 Pos2;

            public readonly Vector3 Coord01;
            public readonly Vector3 Coord02;
            public readonly Vector3 Coord12;

            public readonly Vector3 Pos01;
            public readonly Vector3 Pos02;
            public readonly Vector3 Pos12;

            public readonly Vector3 CoordCross;
            public readonly float CoordCrossSq;

            public readonly float CoordEdge01Sq;
            public readonly float CoordEdge12Sq;
            public readonly float CoordEdge02Sq;
            public readonly float CoordMaxEdgeSq;

            public readonly Vector3 GeometryCross;
            public readonly float GeometryCrossSq;

            public readonly float GeometryEdge01Sq;
            public readonly float GeometryEdge12Sq;
            public readonly float GeometryEdge02Sq;
            public readonly float GeometryMaxEdgeSq;
        }

        #endregion

        private MeshVisualTriangleHoleFillCoordMode _coordMode;
        private MeshVisualTriangleHoleFillEdgeMode _edgeMode;

        private bool _rejectInsideVertices = false;
        private bool _rejectCoveredCenter = false;
        private bool _rejectEdgeIntersections = false;

        private const float _barycentricEpsilon = 1e-4f;
        private const float _edgeInteriorEpsilon = 1e-4f;
        private const float _edgeBoundaryEpsilon = 4e-4f;

        private const float _planeTolerance = 0.0f;
        private const float _planeToleranceFactor = 0.025f;

        private const float _queryPadding = 0.01f;

        private int _atlasSize;
        private int _maxPasses;
        private int _maxAddedTriangles;

        private float _minCoordArea;
        private float _minGeometryArea;

        private float _maxCoordEdgeLength;
        private float _maxGeometryEdgeLength;

        private float _maxEdgeFactor;
        private float _twoEdgeMaxEdgeFactor;

        private float _minNormalDot;
        private bool _fixWinding;

        private float _spatialCellSize;
        private float _spatialCellFactor;

        private bool _rejectCoveredArea;
        private float _maxCoveredAreaRatio;


       // private float _minCoveredArea;


        private VertexData[] _vertices = Array.Empty<VertexData>();
        private Vector3[] _coords = Array.Empty<Vector3>();
        private Triangle[] _triangles = Array.Empty<Triangle>();
        private Vector3[] _normalSums = Array.Empty<Vector3>();

        private HashSet<ulong> _edgeSet = new();
        private HashSet<MeshTriangleKey> _triangleSet = new();
        private HashSet<MeshTriangleKey> _candidateVisited = new();

        private Dictionary<Vector3I, int> _gridHeads = new();
        private int[] _gridTriangles = Array.Empty<int>();
        private int[] _gridNext = Array.Empty<int>();
        private int _gridCount;

        private int[] _queryStamp = Array.Empty<int>();
        private int _queryStampId;

        private int[] _degree = Array.Empty<int>();
        private int[] _offsets = Array.Empty<int>();
        private int[] _cursor = Array.Empty<int>();
        private int[] _adjacency = Array.Empty<int>();

        private int _vertexCount;
        private int _triCount;
        private int _maxAddedLimit;

        private float _avgCoordEdge;
        private float _invCellSize;
        private float _minCoordCrossSq;
        private float _minGeometryCrossSq;
        private float _maxGeometryEdgeSq;
        private float _currentMaxCoordEdgeSq;

        private List<AddedTriangle> _result = new();

#if MESH_HOLE_FILL_DIAGNOSTICS
        private int _diagAcceptedCount;
#endif

        public MeshHoleFiller()
        {
            SetParameters(new MeshHoleFillerParams());
        }

        public MeshHoleFiller(MeshHoleFillerParams parameters)
        {
            SetParameters(parameters);
        }

        public void SetParameters(MeshHoleFillerParams parameters)
        {
            _coordMode = parameters.CoordMode;
            _edgeMode = parameters.EdgeMode;

            _atlasSize = Math.Max(1, parameters.AtlasSize);
            _maxPasses = Math.Max(1, parameters.MaxPasses);
            _maxAddedTriangles = Math.Max(0, parameters.MaxAddedTriangles);

            _minGeometryArea = Math.Max(0.0f, parameters.MinGeometryArea);

            _maxGeometryEdgeLength = Math.Max(0.0f, parameters.MaxGeometryEdgeLength);

            _maxEdgeFactor = Math.Max(0.0f, parameters.MaxEdgeFactor);
            _twoEdgeMaxEdgeFactor = Math.Max(0.0f, parameters.TwoEdgeMaxEdgeFactor);

            _minNormalDot = Math.Clamp(parameters.MinNormalDot, 0.0f, 1.0f);
            _fixWinding = parameters.FixWinding;

            // Non-area rejection tuning is intentionally kept private/dormant for later reactivation.
            // _rejectInsideVertices = parameters.RejectInsideVertices;
            // _rejectCoveredCenter = parameters.RejectCoveredCenter;
            // _rejectEdgeIntersections = parameters.RejectEdgeIntersections;

            // _barycentricEpsilon = Math.Max(0.0f, parameters.BarycentricEpsilon);
            // _edgeInteriorEpsilon = Math.Max(0.0f, parameters.EdgeInteriorEpsilon);
            // _edgeBoundaryEpsilon = Math.Max(0.0f, parameters.EdgeBoundaryEpsilon);

            // _planeTolerance = Math.Max(0.0f, parameters.PlaneTolerance);
            // _planeToleranceFactor = Math.Max(0.0f, parameters.PlaneToleranceFactor);

            // _queryPadding = Math.Max(0.0f, parameters.QueryPadding);


            _rejectCoveredArea = parameters.RejectCoveredArea;
            _maxCoveredAreaRatio = Math.Clamp(parameters.MaxCoveredAreaRatio, 0.0f, 1.0f);
           // _minCoveredArea = Math.Max(0.0f, parameters.MinCoveredArea);
        }

        public List<AddedTriangle> FindMissingTriangles(Geometry3D geometry)
        {
            var result = FindMissingTriangles(geometry.Vertices!, geometry.Indices!);

            if (result.Count == 0)
                return result;

            var curIndices = geometry.Indices!;
            var curIndicesLen = curIndices.Length;

            Array.Resize(ref curIndices, curIndicesLen + result.Count * 3);

            for (var i = 0; i < result.Count; i++)
            {
                var dst = curIndicesLen + i * 3;
                var tri = result[i];

                curIndices[dst + 0] = tri.A;
                curIndices[dst + 1] = tri.B;
                curIndices[dst + 2] = tri.C;
                //Log.Debug(this, "{0} {1} {1}", tri.A, tri.B, tri.C);
            }

            geometry.Indices = curIndices;
            geometry.NotifyChanged(ChangeType.Geometry);

            return result;
        }

        public List<AddedTriangle> FindMissingTriangles(VertexData[] vertices, IReadOnlyList<uint> indices)
        {
            return FindMissingTriangles(vertices, indices, 0, indices.Count);
        }

        public List<AddedTriangle> FindMissingTriangles(VertexData[] vertices, IReadOnlyList<uint> indices, int indexStart, int indexCount)
        {
            if (vertices == null)
                throw new ArgumentNullException(nameof(vertices));

            if (indices == null)
                throw new ArgumentNullException(nameof(indices));

            if (indexStart < 0 || indexStart > indices.Count)
                throw new ArgumentOutOfRangeException(nameof(indexStart));

            if (indexCount < 0 || indexStart + indexCount > indices.Count)
                throw new ArgumentOutOfRangeException(nameof(indexCount));

            indexCount -= indexCount % 3;

            if (indexCount == 0 || vertices.Length == 0)
                return new List<AddedTriangle>(0);

            BeginOperation(vertices, indexCount / 3);

            BuildCoords();
            BuildInputTriangles(indices, indexStart, indexCount);

            if (_triCount == 0 || _edgeSet.Count == 0)
                return _result;

            _avgCoordEdge = ComputeAverageEdgeLength();
            var cellSize = _spatialCellSize > 0.0f ? _spatialCellSize : _avgCoordEdge * _spatialCellFactor;

            if (cellSize <= 1e-10f)
                cellSize = 1.0f;

            _invCellSize = 1.0f / cellSize;

            BuildGrid();

            for (var pass = 0; pass < _maxPasses && _result.Count < _maxAddedLimit; pass++)
            {
                BuildAdjacency();

                var addedThisPass = 0;

                if (_edgeMode != MeshVisualTriangleHoleFillEdgeMode.TwoEdges)
                    addedThisPass += RunMode(false);

                if (addedThisPass == 0 && _edgeMode != MeshVisualTriangleHoleFillEdgeMode.ThreeEdges)
                    addedThisPass += RunMode(true);

                if (addedThisPass == 0)
                    break;

                if (_edgeMode == MeshVisualTriangleHoleFillEdgeMode.ThreeEdges)
                    break;
            }

            return _result;
        }

        private void BeginOperation(VertexData[] vertices, int inputTriCapacity)
        {
            _vertices = vertices;
            _vertexCount = vertices.Length;
            _triCount = 0;
            _gridCount = 0;
            _queryStampId = 1;

            _maxAddedLimit = _maxAddedTriangles == 0 ? int.MaxValue : _maxAddedTriangles;

            var initialExtra = Math.Min(_maxAddedLimit, Math.Max(1024, inputTriCapacity / 8));
            var triCapacity = inputTriCapacity + initialExtra;

            var addedCapacity = Math.Max(16, Math.Min(_maxAddedLimit, Math.Max(256, inputTriCapacity / 64)));
            _result = new List<AddedTriangle>(addedCapacity);

            EnsureCapacity(ref _coords, _vertexCount);
            EnsureCapacity(ref _triangles, triCapacity);
            EnsureCapacity(ref _normalSums, _vertexCount);
            EnsureCapacity(ref _queryStamp, triCapacity);
            EnsureCapacity(ref _degree, _vertexCount);
            EnsureCapacity(ref _offsets, _vertexCount + 1);
            EnsureCapacity(ref _cursor, _vertexCount);

            Array.Clear(_normalSums, 0, _vertexCount);
            Array.Clear(_queryStamp, 0, Math.Min(_queryStamp.Length, triCapacity));

            _edgeSet.Clear();
            _triangleSet.Clear();
            _candidateVisited.Clear();
            _gridHeads.Clear();

            _edgeSet.EnsureCapacity(Math.Max(16, inputTriCapacity * 3));
            _triangleSet.EnsureCapacity(Math.Max(16, inputTriCapacity * 2));
            _gridHeads.EnsureCapacity(Math.Max(16, inputTriCapacity * 2));

            EnsureCapacity(ref _gridTriangles, Math.Max(64, triCapacity * 6));
            EnsureCapacity(ref _gridNext, Math.Max(64, triCapacity * 6));

            _minCoordCrossSq = _minCoordArea > 0.0f ? _minCoordArea * _minCoordArea * 4.0f : 0.0f;
            _minGeometryCrossSq = _minGeometryArea > 0.0f ? _minGeometryArea * _minGeometryArea * 4.0f : 0.0f;
            _maxGeometryEdgeSq = _maxGeometryEdgeLength > 0.0f ? _maxGeometryEdgeLength * _maxGeometryEdgeLength : float.MaxValue;

#if MESH_HOLE_FILL_DIAGNOSTICS
            _diagAcceptedCount = 0;
#endif
        }

        private void BuildCoords()
        {
            if (_coordMode == MeshVisualTriangleHoleFillCoordMode.Uv)
            {
                for (var i = 0; i < _vertexCount; i++)
                    _coords[i] = new Vector3(_vertices[i].UV.X * _atlasSize, _vertices[i].UV.Y * _atlasSize, 0.0f);
            }
            else
            {
                for (var i = 0; i < _vertexCount; i++)
                    _coords[i] = _vertices[i].Pos;
            }
        }

        private void BuildInputTriangles(IReadOnlyList<uint> indices, int indexStart, int indexCount)
        {
            for (var i = 0; i < indexCount; i += 3)
            {
                var ia = (int)indices[indexStart + i + 0];
                var ib = (int)indices[indexStart + i + 1];
                var ic = (int)indices[indexStart + i + 2];

                if ((uint)ia >= _vertexCount || (uint)ib >= _vertexCount || (uint)ic >= _vertexCount)
                    continue;

                if (ia == ib || ib == ic || ia == ic)
                    continue;

                var key = new MeshTriangleKey(ia, ib, ic);

                if (!_triangleSet.Add(key))
                    continue;

                EnsureCapacity(ref _triangles, _triCount + 1);

                _triangles[_triCount] = new Triangle(ia, ib, ic, _coords);
                _triCount++;

                _edgeSet.Add(new MeshEdgeKey(ia, ib).Packed);
                _edgeSet.Add(new MeshEdgeKey(ib, ic).Packed);
                _edgeSet.Add(new MeshEdgeKey(ic, ia).Packed);

                var n = Vector3.Cross(_vertices[ib].Pos - _vertices[ia].Pos, _vertices[ic].Pos - _vertices[ia].Pos);

                if (n.LengthSquared() > 1e-20f)
                {
                    _normalSums[ia] += n;
                    _normalSums[ib] += n;
                    _normalSums[ic] += n;
                }
            }
        }

        private float ComputeAverageEdgeLength()
        {
            var sum = 0.0;
            var count = 0;

            foreach (var edge in _edgeSet)
            {
                var a = (int)(edge >> 32);
                var b = (int)(edge & 0xffffffff);

                var lenSq = (_coords[b] - _coords[a]).LengthSquared();

                if (lenSq <= 1e-20f)
                    continue;

                sum += Math.Sqrt(lenSq);
                count++;
            }

            if (count == 0)
                return 1.0f;

            var avg = (float)(sum / count);

            return avg <= 1e-10f ? 1.0f : avg;
        }

        private void BuildGrid()
        {
            _gridHeads.Clear();
            _gridHeads.EnsureCapacity(Math.Max(16, _triCount * 2));
            _gridCount = 0;

            for (var i = 0; i < _triCount; i++)
                InsertTriangleToGrid(i, _triangles[i]);
        }

        private void BuildAdjacency()
        {
            Array.Clear(_degree, 0, _vertexCount);

            foreach (var edge in _edgeSet)
            {
                var a = (int)(edge >> 32);
                var b = (int)(edge & 0xffffffff);

                _degree[a]++;
                _degree[b]++;
            }

            _offsets[0] = 0;

            for (var i = 0; i < _vertexCount; i++)
                _offsets[i + 1] = _offsets[i] + _degree[i];

            var adjacencyCount = _offsets[_vertexCount];

            EnsureCapacity(ref _adjacency, adjacencyCount);

            Array.Copy(_offsets, _cursor, _vertexCount);

            foreach (var edge in _edgeSet)
            {
                var a = (int)(edge >> 32);
                var b = (int)(edge & 0xffffffff);

                _adjacency[_cursor[a]++] = b;
                _adjacency[_cursor[b]++] = a;
            }
        }
        private struct CandidateSeed
        {
            public int A;
            public int B;
            public int C;
            public int Support;
        }
        private int RunMode(bool twoEdges)
        {
            var addedThisRun = 0;
            var maxCoordEdge = _maxCoordEdgeLength;

            if (maxCoordEdge <= 0.0f)
            {
                var factor = twoEdges ? _twoEdgeMaxEdgeFactor : _maxEdgeFactor;

                if (factor > 0.0f)
                    maxCoordEdge = _avgCoordEdge * factor;
            }

            _currentMaxCoordEdgeSq = maxCoordEdge > 0.0f ? maxCoordEdge * maxCoordEdge : float.MaxValue;

            var buckets = new List<CandidateSeed>?[_vertexCount];

            Parallel.For(0, _vertexCount, b =>
            {
                var start = _offsets[b];
                var end = _offsets[b + 1];

                if (end - start < 2)
                    return;

                List<CandidateSeed>? local = null;

                for (var i = start; i < end - 1; i++)
                {
                    var a = _adjacency[i];

                    for (var j = i + 1; j < end; j++)
                    {
                        var c = _adjacency[j];

                        if (a == c)
                            continue;

                        var hasClosingEdge = _edgeSet.Contains(new MeshEdgeKey(a, c).Packed);

                        if (twoEdges)
                        {
                            if (hasClosingEdge)
                                continue;
                        }
                        else
                        {
                            if (!hasClosingEdge)
                                continue;
                        }

                        local ??= new List<CandidateSeed>();

                        local.Add(new CandidateSeed
                        {
                            A = a,
                            B = b,
                            C = c,
                            Support = twoEdges ? 2 : 3
                        });
                    }
                }

                buckets[b] = local;
            });

            _candidateVisited.Clear();
            _candidateVisited.EnsureCapacity(Math.Max(1024, _edgeSet.Count / 4));

            for (var b = 0; b < buckets.Length; b++)
            {
                var local = buckets[b];

                if (local == null)
                    continue;

                for (var i = 0; i < local.Count; i++)
                {
                    var candidate = local[i];

                    if (TryAddCandidate(candidate.A, candidate.B, candidate.C, candidate.Support))
                        addedThisRun++;

                    if (_result.Count >= _maxAddedLimit)
                        return addedThisRun;
                }
            }

            return addedThisRun;
        }

        private bool TryAddCandidate(int a, int b, int c, int support)
        {
            var candidateKey = new MeshTriangleKey(a, b, c);

            if (!_candidateVisited.Add(candidateKey))
                return false;

            if (_triangleSet.Contains(candidateKey))
                return false;

            var candidate = new Candidate(a, b, c, support, _coords, _vertices);

            if (!PassBasicCandidateChecks(candidate))
                return false;

            if (!TryGetCandidateNormalAndWinding(candidate, out var candidateNormal, out var swapWinding))
                return false;

            if (RejectByNearbyGeometry(candidate, candidateNormal))
                return false;

            var outA = candidate.A;
            var outB = swapWinding ? candidate.C : candidate.B;
            var outC = swapWinding ? candidate.B : candidate.C;

            if (!_triangleSet.Add(candidateKey))
                return false;

            EnsureCapacity(ref _triangles, _triCount + 1);
            EnsureCapacity(ref _queryStamp, _triCount + 1);

            _triangles[_triCount] = new Triangle(outA, outB, outC, _coords);
            InsertTriangleToGrid(_triCount, _triangles[_triCount]);
            _triCount++;

            _edgeSet.Add(new MeshEdgeKey(outA, outB).Packed);
            _edgeSet.Add(new MeshEdgeKey(outB, outC).Packed);
            _edgeSet.Add(new MeshEdgeKey(outC, outA).Packed);

            var n = Vector3.Cross(_vertices[outB].Pos - _vertices[outA].Pos, _vertices[outC].Pos - _vertices[outA].Pos);

            if (n.LengthSquared() > 1e-20f)
            {
                _normalSums[outA] += n;
                _normalSums[outB] += n;
                _normalSums[outC] += n;
            }

            _result.Add(new AddedTriangle((uint)outA, (uint)outB, (uint)outC, support));

            LogAccept(candidate);

            return true;
        }

        private bool PassBasicCandidateChecks(Candidate candidate)
        {
            if (candidate.CoordCrossSq <= _minCoordCrossSq || candidate.CoordCrossSq <= 1e-20f)
                return false;

            if (candidate.CoordMaxEdgeSq > _currentMaxCoordEdgeSq)
                return false;

            if (candidate.GeometryCrossSq <= _minGeometryCrossSq || candidate.GeometryCrossSq <= 1e-20f)
                return false;

            if (candidate.GeometryMaxEdgeSq > _maxGeometryEdgeSq)
                return false;

            return true;
        }

        private bool TryGetCandidateNormalAndWinding(Candidate candidate, out Vector3 candidateCoordNormal, out bool swapWinding)
        {
            swapWinding = false;
            candidateCoordNormal = Vector3.Zero;

            if (candidate.CoordCrossSq <= 1e-20f)
                return false;

            candidateCoordNormal = candidate.CoordCross / MathF.Sqrt(candidate.CoordCrossSq);

            var expectedNormal = _normalSums[candidate.A] + _normalSums[candidate.B] + _normalSums[candidate.C];
            var expectedNormalSq = expectedNormal.LengthSquared();

            if (expectedNormalSq <= 1e-20f)
                return true;

            var realNormal = candidate.GeometryCross / MathF.Sqrt(candidate.GeometryCrossSq);
            expectedNormal /= MathF.Sqrt(expectedNormalSq);

            var dot = Vector3.Dot(realNormal, expectedNormal);

            if (_minNormalDot > 0.0f && MathF.Abs(dot) < _minNormalDot)
                return false;

            if (_fixWinding && dot < 0.0f)
                swapWinding = true;

            return true;
        }

        private bool RejectByNearbyGeometry(Candidate candidate, Vector3 candidateNormal)
        {
            if (!_rejectCoveredArea)
                return false;

            var candidateMaxEdge = MathF.Sqrt(candidate.CoordMaxEdgeSq);
            var planeTolerance = MathF.Max(_planeTolerance, candidateMaxEdge * _planeToleranceFactor);
            var queryPadding = MathF.Max(_queryPadding, planeTolerance);

            var qMin = candidate.CoordMin - new Vector3(queryPadding);
            var qMax = candidate.CoordMax + new Vector3(queryPadding);

            var candidateArea = MathF.Sqrt(candidate.CoordCrossSq) * 0.5f;
            var coveredArea = 0.0f;
            var coveredLimit = candidateArea * _maxCoveredAreaRatio;

            var minCellX = CellCoord(qMin.X, _invCellSize);
            var minCellY = CellCoord(qMin.Y, _invCellSize);
            var minCellZ = CellCoord(qMin.Z, _invCellSize);

            var maxCellX = CellCoord(qMax.X, _invCellSize);
            var maxCellY = CellCoord(qMax.Y, _invCellSize);
            var maxCellZ = CellCoord(qMax.Z, _invCellSize);

            _queryStampId++;

            if (_queryStampId == int.MaxValue)
            {
                Array.Clear(_queryStamp, 0, _queryStamp.Length);
                _queryStampId = 1;
            }

            for (var gx = minCellX; gx <= maxCellX; gx++)
            {
                for (var gy = minCellY; gy <= maxCellY; gy++)
                {
                    for (var gz = minCellZ; gz <= maxCellZ; gz++)
                    {
                        if (!_gridHeads.TryGetValue(new Vector3I(gx, gy, gz), out var entry))
                            continue;

                        while (entry >= 0)
                        {
                            var triId = _gridTriangles[entry];
                            entry = _gridNext[entry];

                            if ((uint)triId >= (uint)_triCount)
                                continue;

                            if (_queryStamp[triId] == _queryStampId)
                                continue;

                            _queryStamp[triId] = _queryStampId;

                            var existing = _triangles[triId];

                            if (_rejectCoveredArea)
                            {
                                var overlapArea = GetProjectedOverlapArea(candidate, existing, candidateNormal, planeTolerance);

                                //LogDiag("covered-area test=({0},{1},{2})", existing.A, existing.B, existing.C);

                                if (overlapArea > 0.0f)
                                {
                                    coveredArea += overlapArea;

                                    LogDiag("covered-area candidate=({0},{1},{2}) tri=({3},{4},{5}) overlap={6} sum={7} limit={8}", candidate.A, candidate.B, candidate.C, existing.A, existing.B, existing.C, overlapArea, coveredArea, coveredLimit);

                                    if (coveredArea >= coveredLimit)
                                    {
                                        LogReject(candidate, "covered-area sum={0} limit={1} ratio={2}", coveredArea, coveredLimit, coveredArea / candidateArea);
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }

        private bool RejectByExistingSamples(Candidate candidate, Triangle existing, Vector3 candidateNormal, float planeTolerance, Vector3 qMin, Vector3 qMax)
        {
            Span<Vector3> samples = stackalloc Vector3[7];
            Span<int> sampleIndices = stackalloc int[7];

            samples[0] = existing.P0;
            samples[1] = existing.P1;
            samples[2] = existing.P2;
            samples[3] = (existing.P0 + existing.P1) * 0.5f;
            samples[4] = (existing.P1 + existing.P2) * 0.5f;
            samples[5] = (existing.P2 + existing.P0) * 0.5f;
            samples[6] = existing.Center;

            sampleIndices[0] = existing.A;
            sampleIndices[1] = existing.B;
            sampleIndices[2] = existing.C;
            sampleIndices[3] = -1;
            sampleIndices[4] = -1;
            sampleIndices[5] = -1;
            sampleIndices[6] = -1;

            for (var i = 0; i < samples.Length; i++)
            {
                var index = sampleIndices[i];

                if (index == candidate.A || index == candidate.B || index == candidate.C)
                    continue;

                var p = samples[i];

                if (p.X < qMin.X || p.Y < qMin.Y || p.Z < qMin.Z ||
                    p.X > qMax.X || p.Y > qMax.Y || p.Z > qMax.Z)
                    continue;

                var signedDistance = Vector3.Dot(p - candidate.Coord0, candidateNormal);

                if (MathF.Abs(signedDistance) > planeTolerance)
                    continue;

                var projected = p - candidateNormal * signedDistance;

                if (TryGetBarycentric(projected, candidate.Coord0, candidate.Coord1, candidate.Coord2, out var u, out var v, out var w) &&
                    u > _barycentricEpsilon &&
                    v > _barycentricEpsilon &&
                    w > _barycentricEpsilon)
                    return true;
            }

            return false;
        }

        private bool RejectByExistingEdges(Candidate candidate, Triangle existing, Vector3 candidateNormal, float planeTolerance)
        {
            for (var i = 0; i < 3; i++)
            {
                var edge = existing.GetEdge(i);

                var d0 = Vector3.Dot(edge.P0 - candidate.Coord0, candidateNormal);
                var d1 = Vector3.Dot(edge.P1 - candidate.Coord0, candidateNormal);

                if (MathF.Abs(d0) > planeTolerance || MathF.Abs(d1) > planeTolerance)
                    continue;

                var p0 = edge.P0 - candidateNormal * d0;
                var p1 = edge.P1 - candidateNormal * d1;

                if (!TryGetBarycentric(p0, candidate.Coord0, candidate.Coord1, candidate.Coord2, out var u0, out var v0, out var w0))
                    continue;

                if (!TryGetBarycentric(p1, candidate.Coord0, candidate.Coord1, candidate.Coord2, out var u1, out var v1, out var w1))
                    continue;

                if (IsOnCandidateBoundary(u0, v0, w0, u1, v1, w1))
                    continue;

                if (HasInteriorSegment(u0, v0, w0, u1, v1, w1, out var tMin, out var tMax))
                {
                    LogDiag("edge-inside existing=({0},{1}) t=({2},{3}) b0=({4},{5},{6}) b1=({7},{8},{9})", edge.A, edge.B, tMin, tMax, u0, v0, w0, u1, v1, w1);
                    return true;
                }
            }

            return false;
        }

        private float GetProjectedOverlapArea(Candidate candidate, Triangle existing, Vector3 candidateNormal, float planeTolerance)
        {
            var normalLenSq = candidateNormal.LengthSquared();

            if (normalLenSq <= 1e-20f)
                return 0.0f;

            candidateNormal /= MathF.Sqrt(normalLenSq);

            var d0 = Vector3.Dot(existing.P0 - candidate.Coord0, candidateNormal);
            var d1 = Vector3.Dot(existing.P1 - candidate.Coord0, candidateNormal);
            var d2 = Vector3.Dot(existing.P2 - candidate.Coord0, candidateNormal);

            var minD = MathF.Min(d0, MathF.Min(d1, d2));
            var maxD = MathF.Max(d0, MathF.Max(d1, d2));

            // Reject only if the existing triangle is completely outside the candidate plane slab.
            // The old code required all 3 vertices to be inside the slab, which kills valid projected overlaps.
            if (minD > planeTolerance || maxD < -planeTolerance)
                return 0.0f;

            var axisX = candidate.Coord1 - candidate.Coord0;
            var axisXLenSq = axisX.LengthSquared();

            if (axisXLenSq <= 1e-20f)
                return 0.0f;

            axisX /= MathF.Sqrt(axisXLenSq);

            var axisY = Vector3.Cross(candidateNormal, axisX);
            var axisYLenSq = axisY.LengthSquared();

            if (axisYLenSq <= 1e-20f)
                return 0.0f;

            axisY /= MathF.Sqrt(axisYLenSq);

            var c0 = Vector2.Zero;

            var dc1 = candidate.Coord1 - candidate.Coord0;
            var dc2 = candidate.Coord2 - candidate.Coord0;

            var c1 = new Vector2(Vector3.Dot(dc1, axisX), Vector3.Dot(dc1, axisY));
            var c2 = new Vector2(Vector3.Dot(dc2, axisX), Vector3.Dot(dc2, axisY));

            var clipArea = Cross2(c1 - c0, c2 - c0);

            if (MathF.Abs(clipArea) <= 1e-20f)
                return 0.0f;

            if (clipArea < 0.0f)
                (c1, c2) = (c2, c1);

            var e0 = existing.P0 - candidate.Coord0;
            var e1 = existing.P1 - candidate.Coord0;
            var e2 = existing.P2 - candidate.Coord0;

            Span<Vector2> poly = stackalloc Vector2[16];
            Span<Vector2> tmp = stackalloc Vector2[16];

            poly[0] = new Vector2(Vector3.Dot(e0, axisX), Vector3.Dot(e0, axisY));
            poly[1] = new Vector2(Vector3.Dot(e1, axisX), Vector3.Dot(e1, axisY));
            poly[2] = new Vector2(Vector3.Dot(e2, axisX), Vector3.Dot(e2, axisY));

            var count = 3;

            count = ClipPolygon(poly, tmp, count, c0, c1);

            if (count == 0)
                return 0.0f;

            count = ClipPolygon(poly, tmp, count, c1, c2);

            if (count == 0)
                return 0.0f;

            count = ClipPolygon(poly, tmp, count, c2, c0);

            if (count == 0)
                return 0.0f;

            var area = 0.0f;

            for (var i = 0; i < count; i++)
            {
                var a = poly[i];
                var b = poly[(i + 1) % count];

                area += a.X * b.Y - b.X * a.Y;
            }

            area = MathF.Abs(area) * 0.5f;

            return area;
        }

        private static int ClipPolygon(Span<Vector2> poly, Span<Vector2> tmp, int count, Vector2 edgeA, Vector2 edgeB)
        {
            const float eps = 1e-7f;

            var outCount = 0;
            var edge = edgeB - edgeA;

            var prev = poly[count - 1];
            var prevSide = Cross2(edge, prev - edgeA);
            var prevInside = prevSide >= -eps;

            for (var i = 0; i < count; i++)
            {
                var cur = poly[i];
                var curSide = Cross2(edge, cur - edgeA);
                var curInside = curSide >= -eps;

                if (curInside != prevInside)
                {
                    var denom = prevSide - curSide;

                    if (MathF.Abs(denom) > 1e-20f)
                    {
                        var t = prevSide / denom;
                        tmp[outCount++] = prev + (cur - prev) * t;
                    }
                }

                if (curInside)
                    tmp[outCount++] = cur;

                prev = cur;
                prevSide = curSide;
                prevInside = curInside;
            }

            for (var i = 0; i < outCount; i++)
                poly[i] = tmp[i];

            return outCount;
        }


        private static float Cross2(Vector2 a, Vector2 b)
        {
            return a.X * b.Y - a.Y * b.X;
        }

        private bool RejectByCoveredCenter(Candidate candidate, Triangle existing, float planeTolerance)
        {
            var normal = Vector3.Cross(existing.P1 - existing.P0, existing.P2 - existing.P0);
            var normalSq = normal.LengthSquared();

            if (normalSq <= 1e-20f)
                return false;

            normal /= MathF.Sqrt(normalSq);

            var center = candidate.CoordCenter;
            var signedDistance = Vector3.Dot(center - existing.P0, normal);

            if (MathF.Abs(signedDistance) > planeTolerance)
                return false;

            var projected = center - normal * signedDistance;

            return TryGetBarycentric(projected, existing.P0, existing.P1, existing.P2, out var u, out var v, out var w) &&
                   u > _barycentricEpsilon &&
                   v > _barycentricEpsilon &&
                   w > _barycentricEpsilon;
        }

        private bool IsOnCandidateBoundary(float u0, float v0, float w0, float u1, float v1, float w1)
        {
            var eps = _edgeBoundaryEpsilon;

            return (MathF.Abs(u0) <= eps && MathF.Abs(u1) <= eps) ||
                   (MathF.Abs(v0) <= eps && MathF.Abs(v1) <= eps) ||
                   (MathF.Abs(w0) <= eps && MathF.Abs(w1) <= eps);
        }

        private bool HasInteriorSegment(float u0, float v0, float w0, float u1, float v1, float w1, out float tMin, out float tMax)
        {
            tMin = 0.0f;
            tMax = 1.0f;

            var eps = _edgeInteriorEpsilon;

            if (!ClipLinearGreaterThan(u0, u1 - u0, eps, ref tMin, ref tMax))
                return false;

            if (!ClipLinearGreaterThan(v0, v1 - v0, eps, ref tMin, ref tMax))
                return false;

            if (!ClipLinearGreaterThan(w0, w1 - w0, eps, ref tMin, ref tMax))
                return false;

            return tMax - tMin > eps;
        }

        private static bool ClipLinearGreaterThan(float start, float delta, float value, ref float tMin, ref float tMax)
        {
            if (MathF.Abs(delta) <= 1e-20f)
                return start > value;

            var t = (value - start) / delta;

            if (delta > 0.0f)
                tMin = MathF.Max(tMin, t);
            else
                tMax = MathF.Min(tMax, t);

            return tMin < tMax;
        }

        private void InsertTriangleToGrid(int triId, Triangle triangle)
        {
            var minCellX = CellCoord(triangle.Min.X, _invCellSize);
            var minCellY = CellCoord(triangle.Min.Y, _invCellSize);
            var minCellZ = CellCoord(triangle.Min.Z, _invCellSize);

            var maxCellX = CellCoord(triangle.Max.X, _invCellSize);
            var maxCellY = CellCoord(triangle.Max.Y, _invCellSize);
            var maxCellZ = CellCoord(triangle.Max.Z, _invCellSize);

            for (var x = minCellX; x <= maxCellX; x++)
            {
                for (var y = minCellY; y <= maxCellY; y++)
                {
                    for (var z = minCellZ; z <= maxCellZ; z++)
                    {
                        EnsureCapacity(ref _gridTriangles, _gridCount + 1);
                        EnsureCapacity(ref _gridNext, _gridCount + 1);

                        var key = new Vector3I(x, y, z);

                        if (!_gridHeads.TryGetValue(key, out var head))
                            head = -1;

                        _gridTriangles[_gridCount] = triId;
                        _gridNext[_gridCount] = head;
                        _gridHeads[key] = _gridCount;

                        _gridCount++;
                    }
                }
            }
        }

        private static int CellCoord(float value, float invCellSize)
        {
            return (int)MathF.Floor(value * invCellSize);
        }

        private static void EnsureCapacity<T>(ref T[] values, int minCapacity)
        {
            if (values.Length >= minCapacity)
                return;

            var newSize = values.Length == 0 ? 16 : values.Length * 2;

            while (newSize < minCapacity)
                newSize *= 2;

            Array.Resize(ref values, newSize);
        }

        private static bool TryGetBarycentric(Vector3 p, Vector3 a, Vector3 b, Vector3 c, out float u, out float v, out float w)
        {
            var v0 = b - a;
            var v1 = c - a;
            var v2 = p - a;

            var d00 = Vector3.Dot(v0, v0);
            var d01 = Vector3.Dot(v0, v1);
            var d11 = Vector3.Dot(v1, v1);
            var d20 = Vector3.Dot(v2, v0);
            var d21 = Vector3.Dot(v2, v1);

            var denom = d00 * d11 - d01 * d01;

            if (MathF.Abs(denom) <= 1e-20f)
            {
                u = 0.0f;
                v = 0.0f;
                w = 0.0f;
                return false;
            }

            var invDenom = 1.0f / denom;

            v = (d11 * d20 - d01 * d21) * invDenom;
            w = (d00 * d21 - d01 * d20) * invDenom;
            u = 1.0f - v - w;

            return true;
        }

        [Conditional("MESH_HOLE_FILL_DIAGNOSTICS")]
        private void LogDiag(string format, params object[] args)
        {
            return;
            Log.Debug(this, string.Format(format, args));
        }

        [Conditional("MESH_HOLE_FILL_DIAGNOSTICS")]
        private void LogAccept(Candidate candidate)
        {
#if MESH_HOLE_FILL_DIAGNOSTICS
            if (_diagAcceptedCount++ >= 64)
                return;

            LogDiag("[HoleFill] ACCEPT ({0},{1},{2}) support={3} area={4} edges=({5},{6},{7})", candidate.A, candidate.B, candidate.C, candidate.Support, MathF.Sqrt(candidate.CoordCrossSq) * 0.5f, MathF.Sqrt(candidate.CoordEdge01Sq), MathF.Sqrt(candidate.CoordEdge12Sq), MathF.Sqrt(candidate.CoordEdge02Sq));
#endif
        }

        [Conditional("MESH_HOLE_FILL_DIAGNOSTICS")]
        private void LogReject(Candidate candidate, string reason, params object[] args)
        {
            LogDiag("[HoleFill] REJECT ({0},{1},{2}) support={3} reason={4}", candidate.A, candidate.B, candidate.C, candidate.Support, string.Format(reason, args));
        }
    }
}
