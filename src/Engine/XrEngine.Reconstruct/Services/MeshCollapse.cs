using System;
using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Numerics;
using System.Numerics;
using XrEngine;

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
            AreaEpsilon = 1e-008f;
            SmallTriangleCollapsePasses = 0;
            RemoveDuplicateTriangles = true;
            FixWinding = true;
            RecoverSingleTriangleHoles = false;
            RecoverSmallBoundaryHoles = false;
            RecoverBoundaryWedges = false;
            MaxRecoverHoleSides = 6;
            MaxRecoverHoleEdgeLength = 0.0f;
            //BoundaryWeldDistance = 0.005f;
            BoundaryWeldDistance = 0f;
        }

        public float Distance { get; set; }

        public float CellSize { get; set; }

        public bool AverageUv { get; set; }

        public bool RecomputeNormals { get; set; }

        public float AreaEpsilon { get; set; }

        public int SmallTriangleCollapsePasses { get; set; }

        public bool RemoveDuplicateTriangles { get; set; }

        public bool FixWinding { get; set; }

        public bool RecoverSingleTriangleHoles { get; set; }

        public bool RecoverSmallBoundaryHoles { get; set; }

        public bool RecoverBoundaryWedges { get; set; }

        public int MaxRecoverHoleSides { get; set; }

        public float MaxRecoverHoleEdgeLength { get; set; }

        public float BoundaryWeldDistance { get; set; }
    }

    public unsafe sealed class MeshCollapse
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
                unchecked
                {
                    var hash = X;
                    hash = (hash * 397) ^ Y;
                    hash = (hash * 397) ^ Z;
                    return hash;
                }
            }

            public readonly int X;

            public readonly int Y;

            public readonly int Z;
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
                unchecked
                {
                    var hash = (int)A;
                    hash = (hash * 397) ^ (int)B;
                    hash = (hash * 397) ^ (int)C;
                    return hash;
                }
            }

            public readonly uint A;

            public readonly uint B;

            public readonly uint C;
        }

        private struct TriangleRef
        {
            public TriangleRef(uint a, uint b, uint c)
            {
                A = a;
                B = b;
                C = c;
            }

            public uint A;

            public uint B;

            public uint C;
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

        private readonly struct CompactResult
        {
            public CompactResult(int oldVertexCount, int newVertexCount)
            {
                OldVertexCount = oldVertexCount;
                NewVertexCount = newVertexCount;
            }

            public int RemovedVertexCount => OldVertexCount - NewVertexCount;

            public int OldVertexCount { get; }

            public int NewVertexCount { get; }
        }

        private readonly struct BoundaryWeldResult
        {
            public BoundaryWeldResult(
                int boundaryVertices,
                int boundaryEdges,
                int nonManifoldEdges,
                int weldedVertices,
                int removedTriangles)
            {
                BoundaryVertices = boundaryVertices;
                BoundaryEdges = boundaryEdges;
                NonManifoldEdges = nonManifoldEdges;
                WeldedVertices = weldedVertices;
                RemovedTriangles = removedTriangles;
            }

            public int BoundaryVertices { get; }

            public int BoundaryEdges { get; }

            public int NonManifoldEdges { get; }

            public int WeldedVertices { get; }

            public int RemovedTriangles { get; }
        }

        private readonly struct DuplicateTriangleResult
        {
            public DuplicateTriangleResult(int removed, int sameWinding, int reversedWinding, int degenerate)
            {
                Removed = removed;
                SameWinding = sameWinding;
                ReversedWinding = reversedWinding;
                Degenerate = degenerate;
            }

            public int Removed { get; }

            public int SameWinding { get; }

            public int ReversedWinding { get; }

            public int Degenerate { get; }
        }

        private struct WindingEdgeUse
        {
            public WindingEdgeUse(int triangle, int direction)
            {
                FirstTriangle = triangle;
                FirstDirection = direction;
                SecondTriangle = -1;
                SecondDirection = 0;
                Count = 1;
            }

            public int FirstTriangle;

            public int FirstDirection;

            public int SecondTriangle;

            public int SecondDirection;

            public int Count;
        }

        private readonly struct WindingConnection
        {
            public WindingConnection(int triangle, int constraint)
            {
                Triangle = triangle;
                Constraint = constraint;
            }

            public int Triangle { get; }

            public int Constraint { get; }
        }

        private readonly struct WindingFixResult
        {
            public WindingFixResult(int flippedTriangles, int manifoldEdges, int nonManifoldEdges, int conflicts)
            {
                FlippedTriangles = flippedTriangles;
                ManifoldEdges = manifoldEdges;
                NonManifoldEdges = nonManifoldEdges;
                Conflicts = conflicts;
            }

            public int FlippedTriangles { get; }

            public int ManifoldEdges { get; }

            public int NonManifoldEdges { get; }

            public int Conflicts { get; }
        }

        private sealed class TopologyCache
        {
            public TopologyCache()
            {
                Edges = new Dictionary<ulong, EdgeInfo>();
                Triangles = new Dictionary<TriangleKey, TriangleRef>();
                BoundaryAdjacency = new Dictionary<uint, List<uint>>();
            }

            public void Clear()
            {
                Edges.Clear();
                Triangles.Clear();
                BoundaryAdjacency.Clear();
                BoundaryEdges = 0;
                NonManifoldEdges = 0;
            }

            public void Build(VertexData[] vertices, uint[] indices, bool buildEdges, bool buildBoundaryAdjacency)
            {
                Clear();

                var triCount = indices.Length / 3;

                Triangles.EnsureCapacity(triCount);

                if (buildEdges)
                    Edges.EnsureCapacity(triCount * 3);

                fixed (uint* pIndices = indices)
                fixed (VertexData* pVertices = vertices)
                {
                    for (var tri = 0; tri < triCount; tri++)
                    {
                        var i = tri * 3;

                        var a = pIndices[i + 0];
                        var b = pIndices[i + 1];
                        var c = pIndices[i + 2];

                        if (a == b || b == c || c == a)
                            continue;

                        var key = new TriangleKey(a, b, c);

                        if (!Triangles.ContainsKey(key))
                            Triangles.Add(key, new TriangleRef(a, b, c));

                        if (!buildEdges)
                            continue;

                        var normal = GetTriangleNormal(pVertices, a, b, c);

                        AddEdge(Edges, a, b, normal);
                        AddEdge(Edges, b, c, normal);
                        AddEdge(Edges, c, a, normal);
                    }
                }

                if (!buildEdges)
                    return;

                foreach (var edge in Edges.Values)
                {
                    if (edge.Count == 1)
                    {
                        BoundaryEdges++;

                        if (buildBoundaryAdjacency)
                        {
                            AddBoundaryAdjacencyUnique(BoundaryAdjacency, edge.A, edge.B);
                            AddBoundaryAdjacencyUnique(BoundaryAdjacency, edge.B, edge.A);
                        }
                    }
                    else if (edge.Count > 2)
                    {
                        NonManifoldEdges++;
                    }
                }
            }

            public Dictionary<ulong, EdgeInfo> Edges { get; }

            public Dictionary<TriangleKey, TriangleRef> Triangles { get; }

            public Dictionary<uint, List<uint>> BoundaryAdjacency { get; }

            public int BoundaryEdges { get; private set; }

            public int NonManifoldEdges { get; private set; }
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
                int collapsedSmallAreaTriangles,
                int removedDuplicateTriangles,
                int fixedWindingTriangles,
                int recoveredSingleTriangleHoles)
            {
                OldVertexCount = oldVertexCount;
                NewVertexCount = newVertexCount;
                OldTriangleCount = oldTriangleCount;
                NewTriangleCount = newTriangleCount;
                RemovedZeroAreaTriangles = removedZeroAreaTriangles;
                CollapsedSmallAreaTriangles = collapsedSmallAreaTriangles;
                RemovedDuplicateTriangles = removedDuplicateTriangles;
                FixedWindingTriangles = fixedWindingTriangles;
                RecoveredSingleTriangleHoles = recoveredSingleTriangleHoles;
            }

            public int OldVertexCount { get; }

            public int NewVertexCount { get; }

            public int OldTriangleCount { get; }

            public int NewTriangleCount { get; }

            public int RemovedZeroAreaTriangles { get; }

            public int CollapsedSmallAreaTriangles { get; }

            public int RemovedDuplicateTriangles { get; }

            public int FixedWindingTriangles { get; }

            public int RecoveredSingleTriangleHoles { get; }
        }

        #endregion

        private const float NormalLengthSqEpsilon = 1E-20f;

        private float _distance;
        private float _cellSize;
        private bool _averageUv;
        private bool _recomputeNormals;
        private float _areaEpsilon;
        private int _smallTriangleCollapsePasses;
        private bool _removeDuplicateTriangles;
        private bool _fixWinding;
        private bool _recoverSingleTriangleHoles;
        private bool _recoverSmallBoundaryHoles;
        private bool _recoverBoundaryWedges;
        private int _maxRecoverHoleSides;
        private float _maxRecoverHoleEdgeLength;
        private float _boundaryWeldDistance;

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
            _smallTriangleCollapsePasses = parameters.SmallTriangleCollapsePasses;
            _removeDuplicateTriangles = parameters.RemoveDuplicateTriangles;
            _fixWinding = parameters.FixWinding;
            _recoverSingleTriangleHoles = parameters.RecoverSingleTriangleHoles;
            _recoverSmallBoundaryHoles = parameters.RecoverSmallBoundaryHoles;
            _recoverBoundaryWedges = parameters.RecoverBoundaryWedges;
            _maxRecoverHoleSides = parameters.MaxRecoverHoleSides;
            _maxRecoverHoleEdgeLength = parameters.MaxRecoverHoleEdgeLength;
            _boundaryWeldDistance = parameters.BoundaryWeldDistance;
        }

        public CollapseResult CollapseCloseVertices(Geometry3D geometry)
        {
            geometry.EnsureIndices();

            var oldVertices = geometry.Vertices;
            var oldIndices = geometry.Indices;
            var oldTriangleCount = oldIndices.Length / 3;

            Log.Debug(this, "MeshCollapse begin: v={0} tri={1}", oldVertices.Length, oldTriangleCount);

            var topology = new TopologyCache();

            var vertexHeads = BuildVertexIndex(oldVertices, out var nextVertexInCell);

            var remap = CollapseVertices(
                oldVertices,
                vertexHeads,
                nextVertexInCell,
                out var newVertices);

            Log.Debug(this, "MeshCollapse vertex-weld: v={0}->{1}", oldVertices.Length, newVertices.Length);

            var vertexRemap = CollapseSmallAreaTriangles(
                oldIndices,
                remap,
                newVertices,
                out var collapsedSmallAreaTriangles);

            Log.Debug(this, "MeshCollapse small-tri-collapse: passes={0} collapsed={1}", _smallTriangleCollapsePasses, collapsedSmallAreaTriangles);

            var newIndices = RebuildTriangles(
                oldIndices,
                remap,
                vertexRemap,
                out var removedZeroAreaTriangles);

            Log.Debug(this, "MeshCollapse rebuild: tri={0}->{1} degenerate={2}", oldTriangleCount, newIndices.Length / 3, removedZeroAreaTriangles);

            var removedDuplicateTriangles = 0;

            if (_removeDuplicateTriangles)
            {
                var duplicateResult = RemoveDuplicateTriangles(ref newIndices, topology);
                removedDuplicateTriangles += duplicateResult.Removed;

                Log.Debug(
                    this,
                    "MeshCollapse duplicates post-collapse: removed={0} same={1} reversed={2} degenerate={3}",
                    duplicateResult.Removed,
                    duplicateResult.SameWinding,
                    duplicateResult.ReversedWinding,
                    duplicateResult.Degenerate);
            }

            var compact = CompactUsedVertices(ref newVertices, newIndices);

            Log.Debug(this, "MeshCollapse compact post-collapse: v={0}->{1}", compact.OldVertexCount, compact.NewVertexCount);

            if (_boundaryWeldDistance > 0.0f)
            {
                var boundaryWeld = BoundaryWeld(newVertices, ref newIndices, topology);

                Log.Debug(
                    this,
                    "MeshCollapse boundary-weld: boundaryV={0} boundaryE={1} nonManifoldE={2} welded={3} removedTri={4}",
                    boundaryWeld.BoundaryVertices,
                    boundaryWeld.BoundaryEdges,
                    boundaryWeld.NonManifoldEdges,
                    boundaryWeld.WeldedVertices,
                    boundaryWeld.RemovedTriangles);

                if (_removeDuplicateTriangles)
                {
                    var duplicateResult = RemoveDuplicateTriangles(ref newIndices, topology);
                    removedDuplicateTriangles += duplicateResult.Removed;

                    Log.Debug(
                        this,
                        "MeshCollapse duplicates post-boundary-weld: removed={0} same={1} reversed={2} degenerate={3}",
                        duplicateResult.Removed,
                        duplicateResult.SameWinding,
                        duplicateResult.ReversedWinding,
                        duplicateResult.Degenerate);
                }

                compact = CompactUsedVertices(ref newVertices, newIndices);

                Log.Debug(this, "MeshCollapse compact post-boundary-weld: v={0}->{1}", compact.OldVertexCount, compact.NewVertexCount);
            }

            var recoveredSingleTriangleHoles = 0;

            if (_recoverSingleTriangleHoles)
            {
                var recovered = RecoverSingleTriangleHoles(newVertices, ref newIndices, topology);
                recoveredSingleTriangleHoles += recovered;

                Log.Debug(this, "MeshCollapse recover-single: added={0} boundaryE={1} nonManifoldE={2}", recovered, topology.BoundaryEdges, topology.NonManifoldEdges);
            }

            if (_recoverBoundaryWedges)
            {
                var recovered = RecoverBoundaryWedges(newVertices, ref newIndices, topology);
                recoveredSingleTriangleHoles += recovered;

                Log.Debug(this, "MeshCollapse recover-wedges: added={0} boundaryE={1} nonManifoldE={2}", recovered, topology.BoundaryEdges, topology.NonManifoldEdges);
            }

            if (_recoverSmallBoundaryHoles)
            {
                var recovered = RecoverSmallBoundaryHoles(newVertices, ref newIndices, topology);
                recoveredSingleTriangleHoles += recovered;

                Log.Debug(this, "MeshCollapse recover-small-loops: added={0} boundaryE={1} nonManifoldE={2}", recovered, topology.BoundaryEdges, topology.NonManifoldEdges);
            }

            if (recoveredSingleTriangleHoles > 0)
            {
                compact = CompactUsedVertices(ref newVertices, newIndices);
                Log.Debug(this, "MeshCollapse compact post-recover: v={0}->{1}", compact.OldVertexCount, compact.NewVertexCount);
            }

            var fixedWindingTriangles = 0;

            if (_fixWinding)
            {
                var windingResult = FixWinding(ref newIndices);
                fixedWindingTriangles = windingResult.FlippedTriangles;

                Log.Debug(
                    this,
                    "MeshCollapse winding: flipped={0} manifoldE={1} nonManifoldE={2} conflicts={3}",
                    windingResult.FlippedTriangles,
                    windingResult.ManifoldEdges,
                    windingResult.NonManifoldEdges,
                    windingResult.Conflicts);
            }

            geometry.Vertices = newVertices;
            geometry.Indices = newIndices;

            if (_recomputeNormals)
            {
                geometry.ComputeNormals();
                Log.Debug(this, "MeshCollapse normals: recomputed");
            }

            Log.Debug(
                this,
                "MeshCollapse done: v={0}->{1} tri={2}->{3} dup={4} winding={5} recovered={6}",
                oldVertices.Length,
                newVertices.Length,
                oldTriangleCount,
                newIndices.Length / 3,
                removedDuplicateTriangles,
                fixedWindingTriangles,
                recoveredSingleTriangleHoles);

            return new CollapseResult(
                oldVertices.Length,
                newVertices.Length,
                oldTriangleCount,
                newIndices.Length / 3,
                removedZeroAreaTriangles,
                collapsedSmallAreaTriangles,
                removedDuplicateTriangles,
                fixedWindingTriangles,
                recoveredSingleTriangleHoles);
        }

        private Dictionary<CellKey, int> BuildVertexIndex(
            VertexData[] vertices,
            out int[] nextVertexInCell)
        {
            var result = new Dictionary<CellKey, int>(vertices.Length);
            nextVertexInCell = new int[vertices.Length];

            fixed (VertexData* pVertices = vertices)
            fixed (int* pNext = nextVertexInCell)
            {
                for (var i = 0; i < vertices.Length; i++)
                {
                    var cell = GetCell(pVertices[i].Pos);

                    if (result.TryGetValue(cell, out var head))
                        pNext[i] = head;
                    else
                        pNext[i] = -1;

                    result[cell] = i;
                }
            }

            return result;
        }

        private uint[] CollapseVertices(
            VertexData[] vertices,
            Dictionary<CellKey, int> vertexHeads,
            int[] nextVertexInCell,
            out VertexData[] newVertices)
        {
            var distanceSq = _distance * _distance;
            var neighborRadius = Math.Max(1, (int)MathF.Ceiling(_distance / _cellSize));

            var remap = new uint[vertices.Length];
            Array.Fill(remap, uint.MaxValue);

            var result = new List<VertexData>(vertices.Length);

            fixed (VertexData* pVertices = vertices)
            fixed (uint* pRemap = remap)
            fixed (int* pNext = nextVertexInCell)
            {
                for (var i = 0; i < vertices.Length; i++)
                {
                    if (pRemap[i] != uint.MaxValue)
                        continue;

                    var anchor = pVertices[i];
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

                                if (!vertexHeads.TryGetValue(key, out var oldIndex))
                                    continue;

                                while (oldIndex >= 0)
                                {
                                    if (pRemap[oldIndex] == uint.MaxValue)
                                    {
                                        var vertex = pVertices[oldIndex];

                                        if (Vector3.DistanceSquared(anchorPos, vertex.Pos) <= distanceSq)
                                        {
                                            pRemap[oldIndex] = newIndex;

                                            sumPos += vertex.Pos;
                                            sumNormal += vertex.Normal;
                                            sumUv += vertex.UV;
                                            count++;
                                        }
                                    }

                                    oldIndex = pNext[oldIndex];
                                }
                            }
                        }
                    }

                    var collapsed = anchor;

                    collapsed.Pos = sumPos / count;

                    if (sumNormal.LengthSquared() > NormalLengthSqEpsilon)
                        collapsed.Normal = Vector3.Normalize(sumNormal);

                    if (_averageUv)
                        collapsed.UV = sumUv / count;

                    result.Add(collapsed);
                }
            }

            newVertices = result.ToArray();

            return remap;
        }

        private uint[] CollapseSmallAreaTriangles(
            uint[] oldIndices,
            uint[] remap,
            VertexData[] vertices,
            out int collapsedSmallAreaTriangles)
        {
            var parent = new uint[vertices.Length];
            var weights = new int[vertices.Length];

            collapsedSmallAreaTriangles = 0;

            fixed (uint* pParent = parent)
            fixed (int* pWeights = weights)
            {
                for (var i = 0; i < vertices.Length; i++)
                {
                    pParent[i] = (uint)i;
                    pWeights[i] = 1;
                }

                if (_areaEpsilon <= 0.0f || _smallTriangleCollapsePasses <= 0 || oldIndices.Length == 0)
                    return parent;

                fixed (uint* pOldIndices = oldIndices)
                fixed (uint* pRemap = remap)
                fixed (VertexData* pVertices = vertices)
                {
                    var triCount = oldIndices.Length / 3;

                    for (var pass = 0; pass < _smallTriangleCollapsePasses; pass++)
                    {
                        var collapsedInPass = 0;

                        for (var tri = 0; tri < triCount; tri++)
                        {
                            var i = tri * 3;

                            var a = Find(pParent, pRemap[(int)pOldIndices[i + 0]]);
                            var b = Find(pParent, pRemap[(int)pOldIndices[i + 1]]);
                            var c = Find(pParent, pRemap[(int)pOldIndices[i + 2]]);

                            if (a == b || b == c || c == a)
                                continue;

                            var areaSq = GetTriangleAreaSq(pVertices, a, b, c);

                            if (areaSq > _areaEpsilon)
                                continue;

                            CollapseShortestEdge(pVertices, pParent, pWeights, a, b, c);

                            collapsedInPass++;
                        }

                        collapsedSmallAreaTriangles += collapsedInPass;

                        if (collapsedInPass == 0)
                            break;
                    }

                    for (var i = 0; i < vertices.Length; i++)
                        pParent[i] = Find(pParent, (uint)i);
                }
            }

            return parent;
        }

        private uint[] RebuildTriangles(
            uint[] oldIndices,
            uint[] remap,
            uint[] vertexRemap,
            out int removedZeroAreaTriangles)
        {
            var result = new uint[oldIndices.Length];
            var write = 0;

            removedZeroAreaTriangles = 0;

            fixed (uint* pOldIndices = oldIndices)
            fixed (uint* pRemap = remap)
            fixed (uint* pVertexRemap = vertexRemap)
            fixed (uint* pResult = result)
            {
                for (var i = 0; i < oldIndices.Length; i += 3)
                {
                    var a = pVertexRemap[(int)pRemap[(int)pOldIndices[i + 0]]];
                    var b = pVertexRemap[(int)pRemap[(int)pOldIndices[i + 1]]];
                    var c = pVertexRemap[(int)pRemap[(int)pOldIndices[i + 2]]];

                    if (a == b || b == c || c == a)
                    {
                        removedZeroAreaTriangles++;
                        continue;
                    }

                    pResult[write++] = a;
                    pResult[write++] = b;
                    pResult[write++] = c;
                }
            }

            if (write == result.Length)
                return result;

            var resized = new uint[write];
            Array.Copy(result, resized, write);

            return resized;
        }

        private DuplicateTriangleResult RemoveDuplicateTriangles(ref uint[] indices, TopologyCache topology)
        {
            var triCount = indices.Length / 3;

            if (triCount == 0)
                return new DuplicateTriangleResult(0, 0, 0, 0);

            topology.Clear();
            topology.Triangles.EnsureCapacity(triCount);

            var result = new uint[indices.Length];
            var write = 0;
            var sameWinding = 0;
            var reversedWinding = 0;
            var degenerate = 0;

            fixed (uint* pIndices = indices)
            fixed (uint* pResult = result)
            {
                for (var tri = 0; tri < triCount; tri++)
                {
                    var i = tri * 3;

                    var a = pIndices[i + 0];
                    var b = pIndices[i + 1];
                    var c = pIndices[i + 2];

                    if (a == b || b == c || c == a)
                    {
                        degenerate++;
                        continue;
                    }

                    var key = new TriangleKey(a, b, c);

                    if (topology.Triangles.TryGetValue(key, out var existing))
                    {
                        if (IsSameWinding(existing.A, existing.B, existing.C, a, b, c))
                            sameWinding++;
                        else
                            reversedWinding++;

                        continue;
                    }

                    topology.Triangles.Add(key, new TriangleRef(a, b, c));

                    pResult[write++] = a;
                    pResult[write++] = b;
                    pResult[write++] = c;
                }
            }

            var removed = sameWinding + reversedWinding + degenerate;

            if (removed == 0)
                return new DuplicateTriangleResult(0, 0, 0, 0);

            var resized = new uint[write];
            Array.Copy(result, resized, write);
            indices = resized;

            return new DuplicateTriangleResult(removed, sameWinding, reversedWinding, degenerate);
        }

        private WindingFixResult FixWinding(ref uint[] indices)
        {
            var triCount = indices.Length / 3;

            if (triCount == 0)
                return new WindingFixResult(0, 0, 0, 0);

            var edges = new Dictionary<ulong, WindingEdgeUse>(triCount * 3);

            fixed (uint* pIndices = indices)
            {
                for (var tri = 0; tri < triCount; tri++)
                {
                    var i = tri * 3;

                    var a = pIndices[i + 0];
                    var b = pIndices[i + 1];
                    var c = pIndices[i + 2];

                    if (a == b || b == c || c == a)
                        continue;

                    AddWindingEdge(edges, a, b, tri);
                    AddWindingEdge(edges, b, c, tri);
                    AddWindingEdge(edges, c, a, tri);
                }
            }

            var adjacency = new List<WindingConnection>?[triCount];
            var manifoldEdges = 0;
            var nonManifoldEdges = 0;

            foreach (var edge in edges.Values)
            {
                if (edge.Count == 2)
                {
                    var constraint = -edge.FirstDirection * edge.SecondDirection;

                    AddWindingConnection(adjacency, edge.FirstTriangle, edge.SecondTriangle, constraint);
                    AddWindingConnection(adjacency, edge.SecondTriangle, edge.FirstTriangle, constraint);

                    manifoldEdges++;
                }
                else if (edge.Count > 2)
                {
                    nonManifoldEdges++;
                }
            }

            var winding = new int[triCount];
            var queue = new Queue<int>();
            var conflicts = 0;

            for (var start = 0; start < triCount; start++)
            {
                if (winding[start] != 0)
                    continue;

                if (adjacency[start] == null)
                    continue;

                winding[start] = 1;
                queue.Enqueue(start);

                while (queue.Count > 0)
                {
                    var tri = queue.Dequeue();
                    var links = adjacency[tri];

                    if (links == null)
                        continue;

                    for (var i = 0; i < links.Count; i++)
                    {
                        var link = links[i];
                        var expected = winding[tri] * link.Constraint;

                        if (winding[link.Triangle] == 0)
                        {
                            winding[link.Triangle] = expected;
                            queue.Enqueue(link.Triangle);
                            continue;
                        }

                        if (winding[link.Triangle] != expected)
                            conflicts++;
                    }
                }
            }

            var flipped = 0;

            fixed (uint* pIndices = indices)
            {
                for (var tri = 0; tri < triCount; tri++)
                {
                    if (winding[tri] >= 0)
                        continue;

                    var i = tri * 3;
                    var tmp = pIndices[i + 1];
                    pIndices[i + 1] = pIndices[i + 2];
                    pIndices[i + 2] = tmp;
                    flipped++;
                }
            }

            return new WindingFixResult(flipped, manifoldEdges, nonManifoldEdges, conflicts / 2);
        }

        private BoundaryWeldResult BoundaryWeld(VertexData[] vertices, ref uint[] indices, TopologyCache topology)
        {
            var triCount = indices.Length / 3;

            if (triCount == 0)
                return new BoundaryWeldResult(0, 0, 0, 0, 0);

            topology.Build(vertices, indices, true, false);

            var boundaryMask = new byte[vertices.Length];
            var boundaryCount = 0;

            fixed (byte* pBoundaryMask = boundaryMask)
            {
                foreach (var edge in topology.Edges.Values)
                {
                    if (edge.Count != 1)
                        continue;

                    if (pBoundaryMask[(int)edge.A] == 0)
                    {
                        pBoundaryMask[(int)edge.A] = 1;
                        boundaryCount++;
                    }

                    if (pBoundaryMask[(int)edge.B] == 0)
                    {
                        pBoundaryMask[(int)edge.B] = 1;
                        boundaryCount++;
                    }
                }
            }

            if (boundaryCount == 0)
                return new BoundaryWeldResult(0, 0, topology.NonManifoldEdges, 0, 0);

            var weldDistanceSq = _boundaryWeldDistance * _boundaryWeldDistance;
            var parent = new uint[vertices.Length];
            var weights = new int[vertices.Length];
            var nextInCell = new int[vertices.Length];
            var cellHeads = new Dictionary<CellKey, int>(boundaryCount * 2);

            fixed (uint* pParent = parent)
            fixed (int* pWeights = weights)
            fixed (int* pNextInCell = nextInCell)
            fixed (byte* pBoundaryMask = boundaryMask)
            fixed (VertexData* pVertices = vertices)
            {
                for (var i = 0; i < vertices.Length; i++)
                {
                    pParent[i] = (uint)i;
                    pWeights[i] = 1;
                }

                for (var i = 0; i < vertices.Length; i++)
                {
                    if (pBoundaryMask[i] == 0)
                        continue;

                    var cell = GetCell(pVertices[i].Pos, _boundaryWeldDistance);

                    if (cellHeads.TryGetValue(cell, out var head))
                        pNextInCell[i] = head;
                    else
                        pNextInCell[i] = -1;

                    cellHeads[cell] = i;
                }

                for (var i = 0; i < vertices.Length; i++)
                {
                    if (pBoundaryMask[i] == 0)
                        continue;

                    var pos = pVertices[i].Pos;
                    var cell = GetCell(pos, _boundaryWeldDistance);

                    for (var z = -1; z <= 1; z++)
                    {
                        for (var y = -1; y <= 1; y++)
                        {
                            for (var x = -1; x <= 1; x++)
                            {
                                var key = new CellKey(
                                    cell.X + x,
                                    cell.Y + y,
                                    cell.Z + z);

                                if (!cellHeads.TryGetValue(key, out var other))
                                    continue;

                                while (other >= 0)
                                {
                                    if (other > i && pBoundaryMask[other] != 0)
                                    {
                                        if (Vector3.DistanceSquared(pos, pVertices[other].Pos) <= weldDistanceSq)
                                            UnionBoundaryRoots(pParent, pWeights, (uint)i, (uint)other);
                                    }

                                    other = pNextInCell[other];
                                }
                            }
                        }
                    }
                }

                var posSums = new Vector3[vertices.Length];
                var normalSums = new Vector3[vertices.Length];
                var uvSums = new Vector2[vertices.Length];
                var counts = new int[vertices.Length];

                fixed (Vector3* pPosSums = posSums)
                fixed (Vector3* pNormalSums = normalSums)
                fixed (Vector2* pUvSums = uvSums)
                fixed (int* pCounts = counts)
                {
                    for (var i = 0; i < vertices.Length; i++)
                    {
                        if (pBoundaryMask[i] == 0)
                            continue;

                        var root = Find(pParent, (uint)i);
                        pParent[i] = root;

                        pPosSums[(int)root] += pVertices[i].Pos;
                        pNormalSums[(int)root] += pVertices[i].Normal;
                        pUvSums[(int)root] += pVertices[i].UV;
                        pCounts[(int)root]++;
                    }

                    var welded = 0;

                    for (var i = 0; i < vertices.Length; i++)
                    {
                        if (pCounts[i] <= 1)
                            continue;

                        var vertex = pVertices[i];

                        vertex.Pos = pPosSums[i] / pCounts[i];

                        if (pNormalSums[i].LengthSquared() > NormalLengthSqEpsilon)
                            vertex.Normal = Vector3.Normalize(pNormalSums[i]);

                        vertex.UV = pUvSums[i] / pCounts[i];

                        pVertices[i] = vertex;
                        welded += pCounts[i] - 1;
                    }

                    var result = new uint[indices.Length];
                    var write = 0;
                    var removedTriangles = 0;

                    fixed (uint* pIndices = indices)
                    fixed (uint* pResult = result)
                    {
                        for (var i = 0; i < indices.Length; i += 3)
                        {
                            var a = pParent[(int)pIndices[i + 0]];
                            var b = pParent[(int)pIndices[i + 1]];
                            var c = pParent[(int)pIndices[i + 2]];

                            if (a == b || b == c || c == a)
                            {
                                removedTriangles++;
                                continue;
                            }

                            pResult[write++] = a;
                            pResult[write++] = b;
                            pResult[write++] = c;
                        }
                    }

                    if (write != indices.Length)
                    {
                        var resized = new uint[write];
                        Array.Copy(result, resized, write);
                        indices = resized;
                    }

                    return new BoundaryWeldResult(
                        boundaryCount,
                        topology.BoundaryEdges,
                        topology.NonManifoldEdges,
                        welded,
                        removedTriangles);
                }
            }
        }

        private int RecoverSingleTriangleHoles(VertexData[] vertices, ref uint[] indices, TopologyCache topology)
        {
            var triCount = indices.Length / 3;

            if (triCount == 0)
                return 0;

            topology.Build(vertices, indices, true, true);

            if (topology.BoundaryAdjacency.Count == 0)
                return 0;

            var result = new List<uint>(indices.Length + topology.BoundaryAdjacency.Count * 3);
            result.AddRange(indices);

            var recovered = 0;
            var maxEdgeSq = _maxRecoverHoleEdgeLength * _maxRecoverHoleEdgeLength;

            fixed (VertexData* pVertices = vertices)
            {
                foreach (var item in topology.Edges.Values)
                {
                    if (item.Count != 1)
                        continue;

                    var a = item.A;
                    var b = item.B;

                    if (!topology.BoundaryAdjacency.TryGetValue(a, out var aEdges))
                        continue;

                    foreach (var c in aEdges)
                    {
                        if (c == a || c == b)
                            continue;

                        if (!HasBoundaryNeighbor(topology.BoundaryAdjacency, b, c))
                            continue;

                        var key = new TriangleKey(a, b, c);

                        if (topology.Triangles.ContainsKey(key))
                            continue;

                        if (_maxRecoverHoleEdgeLength > 0.0f)
                        {
                            if (Vector3.DistanceSquared(pVertices[(int)a].Pos, pVertices[(int)b].Pos) > maxEdgeSq)
                                continue;

                            if (Vector3.DistanceSquared(pVertices[(int)b].Pos, pVertices[(int)c].Pos) > maxEdgeSq)
                                continue;

                            if (Vector3.DistanceSquared(pVertices[(int)c].Pos, pVertices[(int)a].Pos) > maxEdgeSq)
                                continue;
                        }

                        var areaSq = GetTriangleAreaSq(pVertices, a, b, c);

                        if (areaSq <= _areaEpsilon)
                            continue;

                        var normal = GetTriangleNormal(pVertices, a, b, c);

                        var neighborNormal =
                            GetEdgeNormal(topology.Edges, a, b) +
                            GetEdgeNormal(topology.Edges, b, c) +
                            GetEdgeNormal(topology.Edges, c, a);

                        topology.Triangles.Add(key, new TriangleRef(a, b, c));

                        if (neighborNormal.LengthSquared() > NormalLengthSqEpsilon && Vector3.Dot(normal, neighborNormal) < 0.0f)
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
            }

            if (recovered > 0)
                indices = result.ToArray();

            return recovered;
        }

        private int RecoverBoundaryWedges(VertexData[] vertices, ref uint[] indices, TopologyCache topology)
        {
            var triCount = indices.Length / 3;

            if (triCount == 0)
                return 0;

            topology.Build(vertices, indices, true, true);

            if (topology.BoundaryAdjacency.Count == 0)
                return 0;

            var result = new List<uint>(indices.Length + topology.BoundaryAdjacency.Count * 3);
            result.AddRange(indices);

            var recovered = 0;
            var maxEdgeSq = _maxRecoverHoleEdgeLength * _maxRecoverHoleEdgeLength;

            fixed (VertexData* pVertices = vertices)
            {
                foreach (var item in topology.BoundaryAdjacency)
                {
                    var m = item.Key;
                    var neighbors = item.Value;

                    if (neighbors.Count != 2)
                        continue;

                    var a = neighbors[0];
                    var b = neighbors[1];

                    if (a == b || a == m || b == m)
                        continue;

                    var key = new TriangleKey(a, m, b);

                    if (topology.Triangles.ContainsKey(key))
                        continue;

                    if (_maxRecoverHoleEdgeLength > 0.0f)
                    {
                        if (Vector3.DistanceSquared(pVertices[(int)a].Pos, pVertices[(int)m].Pos) > maxEdgeSq)
                            continue;

                        if (Vector3.DistanceSquared(pVertices[(int)m].Pos, pVertices[(int)b].Pos) > maxEdgeSq)
                            continue;

                        if (Vector3.DistanceSquared(pVertices[(int)b].Pos, pVertices[(int)a].Pos) > maxEdgeSq)
                            continue;
                    }

                    var areaSq = GetTriangleAreaSq(pVertices, a, m, b);

                    if (areaSq <= _areaEpsilon)
                        continue;

                    var normal = GetTriangleNormal(pVertices, a, m, b);

                    var neighborNormal =
                        GetEdgeNormal(topology.Edges, a, m) +
                        GetEdgeNormal(topology.Edges, m, b);

                    topology.Triangles.Add(key, new TriangleRef(a, m, b));

                    if (neighborNormal.LengthSquared() > NormalLengthSqEpsilon && Vector3.Dot(normal, neighborNormal) < 0.0f)
                    {
                        result.Add(a);
                        result.Add(b);
                        result.Add(m);
                    }
                    else
                    {
                        result.Add(a);
                        result.Add(m);
                        result.Add(b);
                    }

                    recovered++;
                }
            }

            if (recovered > 0)
                indices = result.ToArray();

            return recovered;
        }

        private int RecoverSmallBoundaryHoles(VertexData[] vertices, ref uint[] indices, TopologyCache topology)
        {
            var triCount = indices.Length / 3;

            if (triCount == 0)
                return 0;

            topology.Build(vertices, indices, true, true);

            if (topology.BoundaryAdjacency.Count == 0)
                return 0;

            var visited = new HashSet<uint>(topology.BoundaryAdjacency.Count);
            var result = new List<uint>(indices.Length + 256 * 3);
            result.AddRange(indices);

            var recovered = 0;

            fixed (VertexData* pVertices = vertices)
            {
                foreach (var start in topology.BoundaryAdjacency.Keys)
                {
                    if (visited.Contains(start))
                        continue;

                    var loop = ExtractClosedBoundaryComponent(topology.BoundaryAdjacency, visited, start);

                    if (loop == null)
                        continue;

                    if (loop.Count < 4 || loop.Count > _maxRecoverHoleSides)
                        continue;

                    if (!AcceptRecoverHoleLoop(pVertices, loop))
                        continue;

                    recovered += AddLoopTriangles(
                        pVertices,
                        topology.Edges,
                        topology.Triangles,
                        result,
                        loop);
                }
            }

            if (recovered > 0)
                indices = result.ToArray();

            return recovered;
        }

        private List<uint>? ExtractClosedBoundaryComponent(
            Dictionary<uint, List<uint>> adjacency,
            HashSet<uint> visited,
            uint start)
        {
            if (!adjacency.TryGetValue(start, out var startEdges))
                return null;

            if (startEdges.Count != 2)
            {
                visited.Add(start);
                return null;
            }

            var loop = new List<uint>(_maxRecoverHoleSides + 1);

            var previous = uint.MaxValue;
            var current = start;

            for (var i = 0; i <= _maxRecoverHoleSides; i++)
            {
                if (!adjacency.TryGetValue(current, out var edges))
                    return null;

                if (edges.Count != 2)
                    return null;

                if (visited.Contains(current))
                {
                    if (current == start && loop.Count >= 3)
                        return loop;

                    return null;
                }

                visited.Add(current);
                loop.Add(current);

                var next = edges[0] == previous ? edges[1] : edges[0];

                previous = current;
                current = next;
            }

            return null;
        }

        private bool AcceptRecoverHoleLoop(VertexData* vertices, List<uint> loop)
        {
            if (_maxRecoverHoleEdgeLength > 0.0f)
            {
                var maxEdgeSq = _maxRecoverHoleEdgeLength * _maxRecoverHoleEdgeLength;

                for (var i = 0; i < loop.Count; i++)
                {
                    var a = loop[i];
                    var b = loop[(i + 1) % loop.Count];

                    if (Vector3.DistanceSquared(vertices[(int)a].Pos, vertices[(int)b].Pos) > maxEdgeSq)
                        return false;
                }
            }

            var normal = Vector3.Zero;
            var origin = vertices[(int)loop[0]].Pos;

            for (var i = 1; i < loop.Count - 1; i++)
            {
                var p1 = vertices[(int)loop[i]].Pos;
                var p2 = vertices[(int)loop[i + 1]].Pos;

                normal += Vector3.Cross(p1 - origin, p2 - origin);
            }

            return normal.LengthSquared() > _areaEpsilon;
        }

        private int AddLoopTriangles(
            VertexData* vertices,
            Dictionary<ulong, EdgeInfo> edges,
            Dictionary<TriangleKey, TriangleRef> triangles,
            List<uint> result,
            List<uint> loop)
        {
            var recovered = 0;
            var neighborNormal = Vector3.Zero;

            for (var i = 0; i < loop.Count; i++)
                neighborNormal += GetEdgeNormal(edges, loop[i], loop[(i + 1) % loop.Count]);

            for (var i = 1; i < loop.Count - 1; i++)
            {
                var a = loop[0];
                var b = loop[i];
                var c = loop[i + 1];

                var key = new TriangleKey(a, b, c);

                if (triangles.ContainsKey(key))
                    continue;

                var areaSq = GetTriangleAreaSq(vertices, a, b, c);

                if (areaSq <= _areaEpsilon)
                    continue;

                var normal = GetTriangleNormal(vertices, a, b, c);

                triangles.Add(key, new TriangleRef(a, b, c));

                if (neighborNormal.LengthSquared() > NormalLengthSqEpsilon && Vector3.Dot(normal, neighborNormal) < 0.0f)
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

            return recovered;
        }

        private CompactResult CompactUsedVertices(ref VertexData[] vertices, uint[] indices)
        {
            var oldVertexCount = vertices.Length;
            var remap = new int[vertices.Length];

            Array.Fill(remap, -1);

            var result = new List<VertexData>(vertices.Length);

            fixed (uint* pIndices = indices)
            fixed (int* pRemap = remap)
            {
                for (var i = 0; i < indices.Length; i++)
                {
                    var oldIndex = (int)pIndices[i];
                    var newIndex = pRemap[oldIndex];

                    if (newIndex < 0)
                    {
                        newIndex = result.Count;
                        result.Add(vertices[oldIndex]);
                        pRemap[oldIndex] = newIndex;
                    }

                    pIndices[i] = (uint)newIndex;
                }
            }

            vertices = result.ToArray();

            return new CompactResult(oldVertexCount, vertices.Length);
        }

        private void CollapseShortestEdge(
            VertexData* vertices,
            uint* parent,
            int* weights,
            uint a,
            uint b,
            uint c)
        {
            var ab = Vector3.DistanceSquared(vertices[(int)a].Pos, vertices[(int)b].Pos);
            var bc = Vector3.DistanceSquared(vertices[(int)b].Pos, vertices[(int)c].Pos);
            var ca = Vector3.DistanceSquared(vertices[(int)c].Pos, vertices[(int)a].Pos);

            if (ab <= bc && ab <= ca)
            {
                UnionVertices(vertices, parent, weights, a, b);
                return;
            }

            if (bc <= ca)
            {
                UnionVertices(vertices, parent, weights, b, c);
                return;
            }

            UnionVertices(vertices, parent, weights, c, a);
        }

        private void UnionVertices(
            VertexData* vertices,
            uint* parent,
            int* weights,
            uint a,
            uint b)
        {
            var rootA = Find(parent, a);
            var rootB = Find(parent, b);

            if (rootA == rootB)
                return;

            if (weights[(int)rootB] > weights[(int)rootA])
                (rootA, rootB) = (rootB, rootA);

            var weightA = weights[(int)rootA];
            var weightB = weights[(int)rootB];
            var totalWeight = weightA + weightB;

            var va = vertices[(int)rootA];
            var vb = vertices[(int)rootB];

            va.Pos = ((va.Pos * weightA) + (vb.Pos * weightB)) / totalWeight;

            var normal = (va.Normal * weightA) + (vb.Normal * weightB);

            if (normal.LengthSquared() > NormalLengthSqEpsilon)
                va.Normal = Vector3.Normalize(normal);

            if (_averageUv)
                va.UV = ((va.UV * weightA) + (vb.UV * weightB)) / totalWeight;

            vertices[(int)rootA] = va;

            parent[(int)rootB] = rootA;
            weights[(int)rootA] = totalWeight;
        }

        private static void UnionBoundaryRoots(
            uint* parent,
            int* weights,
            uint a,
            uint b)
        {
            var rootA = Find(parent, a);
            var rootB = Find(parent, b);

            if (rootA == rootB)
                return;

            if (weights[(int)rootB] > weights[(int)rootA])
                (rootA, rootB) = (rootB, rootA);

            parent[(int)rootB] = rootA;
            weights[(int)rootA] += weights[(int)rootB];
        }

        private static uint Find(uint* parent, uint index)
        {
            var root = index;

            while (parent[(int)root] != root)
                root = parent[(int)root];

            while (parent[(int)index] != index)
            {
                var next = parent[(int)index];
                parent[(int)index] = root;
                index = next;
            }

            return root;
        }

        private static float GetTriangleAreaSq(
            VertexData* vertices,
            uint a,
            uint b,
            uint c)
        {
            var p0 = vertices[(int)a].Pos;
            var p1 = vertices[(int)b].Pos;
            var p2 = vertices[(int)c].Pos;

            return Vector3.Cross(p1 - p0, p2 - p0).LengthSquared();
        }

        private static Vector3 GetTriangleNormal(
            VertexData* vertices,
            uint a,
            uint b,
            uint c)
        {
            var p0 = vertices[(int)a].Pos;
            var p1 = vertices[(int)b].Pos;
            var p2 = vertices[(int)c].Pos;

            return Vector3.Cross(p1 - p0, p2 - p0);
        }

        private static void AddEdge(
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

        private static void AddWindingEdge(
            Dictionary<ulong, WindingEdgeUse> edges,
            uint from,
            uint to,
            int triangle)
        {
            var key = EdgeKey(from, to);
            var direction = from < to ? 1 : -1;

            if (edges.TryGetValue(key, out var item))
            {
                if (item.Count == 1)
                {
                    item.SecondTriangle = triangle;
                    item.SecondDirection = direction;
                }

                item.Count++;
                edges[key] = item;
                return;
            }

            edges[key] = new WindingEdgeUse(triangle, direction);
        }

        private static void AddWindingConnection(
            List<WindingConnection>?[] adjacency,
            int fromTriangle,
            int toTriangle,
            int constraint)
        {
            var list = adjacency[fromTriangle];

            if (list == null)
            {
                list = new List<WindingConnection>(3);
                adjacency[fromTriangle] = list;
            }

            list.Add(new WindingConnection(toTriangle, constraint));
        }

        private static void AddBoundaryAdjacencyUnique(
            Dictionary<uint, List<uint>> adjacency,
            uint a,
            uint b)
        {
            if (!adjacency.TryGetValue(a, out var list))
            {
                list = new List<uint>(2);
                adjacency[a] = list;
            }

            for (var i = 0; i < list.Count; i++)
            {
                if (list[i] == b)
                    return;
            }

            list.Add(b);
        }

        private static bool HasBoundaryNeighbor(
            Dictionary<uint, List<uint>> adjacency,
            uint a,
            uint b)
        {
            if (!adjacency.TryGetValue(a, out var list))
                return false;

            for (var i = 0; i < list.Count; i++)
            {
                if (list[i] == b)
                    return true;
            }

            return false;
        }

        private static Vector3 GetEdgeNormal(
            Dictionary<ulong, EdgeInfo> edges,
            uint a,
            uint b)
        {
            if (!edges.TryGetValue(EdgeKey(a, b), out var item))
                return Vector3.Zero;

            return item.NormalSum;
        }

        private static ulong EdgeKey(uint a, uint b)
        {
            if (a > b)
                (a, b) = (b, a);

            return ((ulong)a << 32) | b;
        }

        private static bool IsSameWinding(
            uint a0,
            uint b0,
            uint c0,
            uint a,
            uint b,
            uint c)
        {
            return
                (a == a0 && b == b0 && c == c0) ||
                (a == b0 && b == c0 && c == a0) ||
                (a == c0 && b == a0 && c == b0);
        }

        private CellKey GetCell(Vector3 pos)
        {
            return GetCell(pos, _cellSize);
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
