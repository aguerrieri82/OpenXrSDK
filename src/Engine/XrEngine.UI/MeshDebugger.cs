
using SkiaSharp;
using System.Diagnostics;
using System.Numerics;
using XrMath;
using static XrEngine.TriangleMeshSpatialIndex;

namespace XrEngine.UI
{
    public class MeshDebugger : Behavior<TriangleMesh>, IDisposable
    {
        TriangleMesh? _slice;
        TriangleMeshSpatialIndex? _index;
        CanvasView2D? _canvas;
        bool _showSlice;
        List<TriangleMeshSpatialIndex.TriangleSearchHit>? _triangles;

        public MeshDebugger()
        {
            CellSize = 0.1f;
            SliceArea = 0.05f;
        }

        protected override void OnAttach()
        {
            Debug.Assert(_host!.Geometry != null);

            _index = new TriangleMeshSpatialIndex(_host.Geometry, CellSize);

            _canvas ??= new CanvasView2D();

            _canvas.DrawCanvas += OnDraw;
        }

        protected override void Start(RenderContext ctx)
        {
            if (_canvas!.Parent == null)
                _host!.Scene!.AddChild(_canvas);

        }

        private void OnDraw(ScreenCanvas ctx)
        {
            //ctx.Canvas.Clear(SKColor.Parse("#ff000080"));

            if (_triangles == null)
                return;

            foreach (var tri in _triangles)
            {
                var value = new Triangle3(tri.Triangle.V0, tri.Triangle.V1, tri.Triangle.V2);

                Color color = !value.IsCCW() ? "#0000ff80" : "#ff000080";

                ctx.Draw(value, color, Color.Black, 2f);

              
            }


            foreach (var tri in _triangles)
            {
                var value = new Triangle3(tri.Triangle.V0, tri.Triangle.V1, tri.Triangle.V2);

                var c = value.Center();

                var eps = c + value.Normal() * 0.05f;

                ctx.Draw(new Line3(c, eps), "#ffff00", 3f);

                ctx.DrawText(tri.Triangle.TriangleId.ToString(), eps, 50f, "#ffffff");
            }

        }

        public void BuildSubMesh(int triangleId)
        {
            if (_index == null)
                return;

            if (_slice == null)
            {
                _slice = new TriangleMesh(new Geometry3D());
                _slice.Materials.Add(new WireframeMaterial() 
                { 
                    Color = new Color(1,0,0), 
                    Priority = 2, 
                    UseDepth = false 
                });
                _slice.Materials.Add(new ColorMaterial() 
                { 
                    Color = new Color(0, 1, 0, 0.8f), 
                    Alpha = AlphaMode.Add, 
                    Priority = 1, 
                    UseDepth = false });

                _slice.Name = "Mesh-Slice";
            
            }

            if (_slice.Parent == null)
                _host!.Scene!.AddChild(_slice);

            var result = _index.SearchAroundTriangle(
                triangleId,
                SliceArea,
                includeSelf: true);

            var source = _index.Geometry;
            var sourceVertices = source.Vertices;

            var vertices = new VertexData[result.Count * 3];
            var indices = new uint[result.Count * 3];

            var write = 0;

            for (var i = 0; i < result.Count; i++)
            {
                var tri = result[i].Triangle;

                vertices[write] = sourceVertices[(int)tri.A];
                indices[write] = (uint)write;
                write++;

                vertices[write] = sourceVertices[(int)tri.B];
                indices[write] = (uint)write;
                write++;

                vertices[write] = sourceVertices[(int)tri.C];
                indices[write] = (uint)write;
                write++;
            }

            Log.Warn(this, "{0} triangles found", result.Count);

            var geometry = _slice.Geometry!;

            geometry.Vertices = vertices;
            geometry.Indices = indices;
            geometry.ActiveComponents = source.ActiveComponents;

            geometry.NotifyChanged(ChangeType.Geometry);

            _slice.UpdateBounds();

            _triangles = result;
        }

        public static void AnalyzeTrianglePatch(
            IList<Triangle> triangles,
            Action<string>? log = null,
            float tJunctionTolerance = 0.0f,
            float sharpNormalDot = 0.25f,
            float foldedNormalDot = -0.25f)
        {
            log ??= Console.WriteLine;

            const float normalEps = 1E-20f;
            const float geomEps = 1E-12f;

            if (triangles.Count == 0)
            {
                log("[patch] empty");
                return;
            }

            var vertexPos = new Dictionary<uint, Vector3>();
            var vertexIncident = new Dictionary<uint, List<int>>();
            var edges = new Dictionary<ulong, List<(int Tri, uint From, uint To, uint Opposite)>>();
            var existingTriangles = new Dictionary<(uint A, uint B, uint C), List<int>>();
            var boundaryAdj = new Dictionary<uint, List<uint>>();
            var edgeLengths = new List<float>(triangles.Count * 3);

            var min = new Vector3(float.PositiveInfinity);
            var max = new Vector3(float.NegativeInfinity);
            var center = Vector3.Zero;
            var normalSum = Vector3.Zero;
            var minAreaSq = float.PositiveInfinity;
            var maxAreaSq = 0.0f;
            var areaSqSum = 0.0f;
            var degenerateCount = 0;

            for (var i = 0; i < triangles.Count; i++)
            {
                var tri = triangles[i];

                min = Vector3.Min(min, tri.Min);
                max = Vector3.Max(max, tri.Max);
                center += tri.Center;

                minAreaSq = MathF.Min(minAreaSq, tri.AreaSq);
                maxAreaSq = MathF.Max(maxAreaSq, tri.AreaSq);
                areaSqSum += tri.AreaSq;

                if (tri.IsDegenerate)
                    degenerateCount++;
                else
                    normalSum += tri.Normal;

                AddVertex(tri.A, tri.V0, i);
                AddVertex(tri.B, tri.V1, i);
                AddVertex(tri.C, tri.V2, i);

                AddTriangleKey(i, tri.A, tri.B, tri.C);

                AddEdge(i, tri.A, tri.B, tri.C);
                AddEdge(i, tri.B, tri.C, tri.A);
                AddEdge(i, tri.C, tri.A, tri.B);

                edgeLengths.Add(Vector3.Distance(tri.V0, tri.V1));
                edgeLengths.Add(Vector3.Distance(tri.V1, tri.V2));
                edgeLengths.Add(Vector3.Distance(tri.V2, tri.V0));
            }

            center /= triangles.Count;

            var avgNormal = Vector3.Zero;

            if (normalSum.LengthSquared() > normalEps)
                avgNormal = Vector3.Normalize(normalSum);

            edgeLengths.Sort();

            var medianEdgeLength = edgeLengths.Count > 0
                ? edgeLengths[edgeLengths.Count / 2]
                : 0.0f;

            if (tJunctionTolerance <= 0.0f && medianEdgeLength > 0.0f)
                tJunctionTolerance = medianEdgeLength * 0.02f;

            log("========== Triangle Patch Analysis ==========");
            log($"[patch] triangles={triangles.Count} vertices={vertexPos.Count} boundsMin=({Fmt(min)}) boundsMax=({Fmt(max)}) center=({Fmt(center)})");
            log($"[area] degenerate={degenerateCount} minAreaSq={minAreaSq:E6} maxAreaSq={maxAreaSq:E6} avgAreaSq={(areaSqSum / triangles.Count):E6}");
            log($"[edges] medianLen={medianEdgeLength:0.000000} tJunctionTol={tJunctionTolerance:0.000000}");
            log($"[normal] avg=({Fmt(avgNormal)})");

            foreach (var item in existingTriangles)
            {
                if (item.Value.Count <= 1)
                    continue;

                log($"[duplicate-triangle] key=({item.Key.A},{item.Key.B},{item.Key.C}) tris={JoinTriangles(item.Value)}");
            }

            var boundaryEdges = 0;
            var manifoldEdges = 0;
            var nonManifoldEdges = 0;
            var windingConflicts = 0;
            var foldedEdges = 0;
            var sharpEdges = 0;

            var neighbors = new List<int>[triangles.Count];

            for (var i = 0; i < neighbors.Length; i++)
                neighbors[i] = new List<int>(4);

            foreach (var pair in edges)
            {
                var list = pair.Value;

                if (list.Count == 1)
                {
                    boundaryEdges++;

                    var e = list[0];

                    AddBoundaryAdj(e.From, e.To);
                    AddBoundaryAdj(e.To, e.From);

                    continue;
                }

                if (list.Count > 2)
                {
                    nonManifoldEdges++;

                    log($"[non-manifold-edge] edge=({EdgeA(pair.Key)},{EdgeB(pair.Key)}) count={list.Count} tris={JoinEdgeTriangles(list)}");
                    continue;
                }

                manifoldEdges++;

                var e0 = list[0];
                var e1 = list[1];

                neighbors[e0.Tri].Add(e1.Tri);
                neighbors[e1.Tri].Add(e0.Tri);

                var sameDirection = e0.From == e1.From && e0.To == e1.To;
                var normalDot = Vector3.Dot(triangles[e0.Tri].Normal, triangles[e1.Tri].Normal);

                if (sameDirection)
                {
                    windingConflicts++;
                    log($"[winding-conflict] edge=({EdgeA(pair.Key)},{EdgeB(pair.Key)}) tris={TriLabel(e0.Tri)} {TriLabel(e1.Tri)} normalDot={normalDot:0.000000}");
                }
                else if (normalDot < foldedNormalDot)
                {
                    foldedEdges++;
                    log($"[folded-edge] edge=({EdgeA(pair.Key)},{EdgeB(pair.Key)}) tris={TriLabel(e0.Tri)} {TriLabel(e1.Tri)} normalDot={normalDot:0.000000}");
                }
                else if (normalDot < sharpNormalDot)
                {
                    sharpEdges++;
                    log($"[sharp-edge] edge=({EdgeA(pair.Key)},{EdgeB(pair.Key)}) tris={TriLabel(e0.Tri)} {TriLabel(e1.Tri)} normalDot={normalDot:0.000000}");
                }
            }

            log($"[topology] boundaryEdges={boundaryEdges} manifoldEdges={manifoldEdges} nonManifoldEdges={nonManifoldEdges}");
            log($"[orientation] windingConflicts={windingConflicts} foldedEdges={foldedEdges} sharpEdges={sharpEdges}");

            var componentIds = new int[triangles.Count];
            Array.Fill(componentIds, -1);

            var componentCount = 0;

            for (var i = 0; i < triangles.Count; i++)
            {
                if (componentIds[i] >= 0)
                    continue;

                var queue = new Queue<int>();
                var comp = new List<int>();

                componentIds[i] = componentCount;
                queue.Enqueue(i);

                while (queue.Count > 0)
                {
                    var tri = queue.Dequeue();
                    comp.Add(tri);

                    var ns = neighbors[tri];

                    for (var n = 0; n < ns.Count; n++)
                    {
                        var other = ns[n];

                        if (componentIds[other] >= 0)
                            continue;

                        componentIds[other] = componentCount;
                        queue.Enqueue(other);
                    }
                }

                var compNormal = Vector3.Zero;
                var compAreaSq = 0.0f;

                for (var j = 0; j < comp.Count; j++)
                {
                    var tri = triangles[comp[j]];

                    if (!tri.IsDegenerate)
                        compNormal += tri.Normal;

                    compAreaSq += tri.AreaSq;
                }

                if (compNormal.LengthSquared() > normalEps)
                    compNormal = Vector3.Normalize(compNormal);

                log($"[component {componentCount}] triangles={comp.Count} areaSq={compAreaSq:E6} normal=({Fmt(compNormal)}) tris={JoinTriangles(comp)}");

                componentCount++;
            }

            log($"[components] count={componentCount}");

            foreach (var item in vertexIncident)
                AnalyzeVertexFans(item.Key, item.Value);

            AnalyzeBoundaryComponents();
            AnalyzeExactThreeEdgeHoleCandidates();
            AnalyzeTwoEdgeWedgeCandidates();
            AnalyzeTJunctions();

            log("========== End Triangle Patch Analysis ==========");

            void AddVertex(uint index, Vector3 pos, int triangleIndex)
            {
                if (vertexPos.TryGetValue(index, out var existing))
                {
                    if (Vector3.DistanceSquared(existing, pos) > geomEps)
                        log($"[vertex-position-conflict] v={index} old=({Fmt(existing)}) new=({Fmt(pos)}) tri={TriLabel(triangleIndex)}");
                }
                else
                {
                    vertexPos.Add(index, pos);
                }

                if (!vertexIncident.TryGetValue(index, out var list))
                {
                    list = new List<int>(8);
                    vertexIncident.Add(index, list);
                }

                list.Add(triangleIndex);
            }

            void AddTriangleKey(int triIndex, uint a, uint b, uint c)
            {
                var key = TriangleKey(a, b, c);

                if (!existingTriangles.TryGetValue(key, out var list))
                {
                    list = new List<int>(1);
                    existingTriangles.Add(key, list);
                }

                list.Add(triIndex);
            }

            void AddEdge(int triIndex, uint from, uint to, uint opposite)
            {
                var key = EdgeKey(from, to);

                if (!edges.TryGetValue(key, out var list))
                {
                    list = new List<(int Tri, uint From, uint To, uint Opposite)>(2);
                    edges.Add(key, list);
                }

                list.Add((triIndex, from, to, opposite));
            }

            void AddBoundaryAdj(uint a, uint b)
            {
                if (!boundaryAdj.TryGetValue(a, out var list))
                {
                    list = new List<uint>(2);
                    boundaryAdj.Add(a, list);
                }

                for (var i = 0; i < list.Count; i++)
                {
                    if (list[i] == b)
                        return;
                }

                list.Add(b);
            }

            void AnalyzeVertexFans(uint vertex, List<int> incident)
            {
                if (incident.Count <= 1)
                    return;

                var localVisited = new bool[incident.Count];
                var fanCount = 0;
                var largestFan = -1;
                var largestFanAreaSq = -1.0f;
                var fanDescriptions = new List<string>();

                for (var i = 0; i < incident.Count; i++)
                {
                    if (localVisited[i])
                        continue;

                    var fan = new List<int>();
                    var queue = new Queue<int>();

                    localVisited[i] = true;
                    queue.Enqueue(i);

                    while (queue.Count > 0)
                    {
                        var local = queue.Dequeue();
                        var tri = incident[local];

                        fan.Add(tri);

                        for (var j = 0; j < incident.Count; j++)
                        {
                            if (localVisited[j])
                                continue;

                            var other = incident[j];

                            if (!ShareEdgeContainingVertex(triangles[tri], triangles[other], vertex))
                                continue;

                            localVisited[j] = true;
                            queue.Enqueue(j);
                        }
                    }

                    var fanAreaSq = 0.0f;
                    var fanNormal = Vector3.Zero;

                    for (var j = 0; j < fan.Count; j++)
                    {
                        var tri = triangles[fan[j]];
                        fanAreaSq += tri.AreaSq;

                        if (!tri.IsDegenerate)
                            fanNormal += tri.Normal;
                    }

                    if (fanNormal.LengthSquared() > normalEps)
                        fanNormal = Vector3.Normalize(fanNormal);

                    if (fanAreaSq > largestFanAreaSq)
                    {
                        largestFanAreaSq = fanAreaSq;
                        largestFan = fanCount;
                    }

                    fanDescriptions.Add($"fan={fanCount} count={fan.Count} areaSq={fanAreaSq:E6} normal=({Fmt(fanNormal)}) tris={JoinTriangles(fan)}");
                    fanCount++;
                }

                if (fanCount <= 1)
                    return;

                log($"[bad-weld-vertex] v={vertex} pos=({Fmt(vertexPos[vertex])}) incident={incident.Count} fans={fanCount} keepFanCandidate={largestFan}");

                for (var i = 0; i < fanDescriptions.Count; i++)
                    log($"    {fanDescriptions[i]}");

                log("    [repair-if-no-vertex-duplication] remove triangles from weaker fans; cannot keep all fans with one welded vertex");
            }

            void AnalyzeBoundaryComponents()
            {
                if (boundaryAdj.Count == 0)
                {
                    log("[boundary-components] count=0");
                    return;
                }

                var visited = new HashSet<uint>();
                var componentId = 0;

                foreach (var start in boundaryAdj.Keys)
                {
                    if (visited.Contains(start))
                        continue;

                    var queue = new Queue<uint>();
                    var verts = new List<uint>();
                    var edgeCount2 = 0;
                    var degree1 = 0;
                    var degree2 = 0;
                    var degreeOther = 0;

                    visited.Add(start);
                    queue.Enqueue(start);

                    while (queue.Count > 0)
                    {
                        var v = queue.Dequeue();
                        verts.Add(v);

                        var list = boundaryAdj[v];
                        edgeCount2 += list.Count;

                        if (list.Count == 1)
                            degree1++;
                        else if (list.Count == 2)
                            degree2++;
                        else
                            degreeOther++;

                        for (var i = 0; i < list.Count; i++)
                        {
                            var n = list[i];

                            if (visited.Add(n))
                                queue.Enqueue(n);
                        }
                    }

                    var edgeCount = edgeCount2 / 2;
                    var closed = degree1 == 0 && degreeOther == 0 && degree2 == verts.Count;

                    log($"[boundary-component {componentId}] vertices={verts.Count} edges={edgeCount} closed={closed} degree1={degree1} degree2={degree2} degreeOther={degreeOther} verts={JoinVertices(verts)}");

                    if (closed && verts.Count == 3)
                        log($"    [closed-3-boundary] this is an exact single-triangle hole candidate if triangle does not already exist");

                    componentId++;
                }

                log($"[boundary-components] count={componentId}");
            }

            void AnalyzeExactThreeEdgeHoleCandidates()
            {
                var boundaryVertices = new List<uint>(boundaryAdj.Keys);
                var found = 0;

                for (var i = 0; i < boundaryVertices.Count; i++)
                {
                    for (var j = i + 1; j < boundaryVertices.Count; j++)
                    {
                        for (var k = j + 1; k < boundaryVertices.Count; k++)
                        {
                            var a = boundaryVertices[i];
                            var b = boundaryVertices[j];
                            var c = boundaryVertices[k];

                            if (!IsBoundaryEdge(a, b) || !IsBoundaryEdge(b, c) || !IsBoundaryEdge(c, a))
                                continue;

                            var triKey = TriangleKey(a, b, c);

                            if (existingTriangles.ContainsKey(triKey))
                                continue;

                            if (!TryGetCandidateGeometry(a, b, c, out var normal, out var areaSq, out var maxEdge))
                                continue;

                            var expected = GetBoundaryEdgeNormal(a, b) + GetBoundaryEdgeNormal(b, c) + GetBoundaryEdgeNormal(c, a);
                            var dot = expected.LengthSquared() > normalEps ? Vector3.Dot(normal, Vector3.Normalize(expected)) : 0.0f;

                            var outA = a;
                            var outB = b;
                            var outC = c;

                            if (expected.LengthSquared() > normalEps && dot < 0.0f)
                            {
                                outB = c;
                                outC = b;
                                dot = -dot;
                            }

                            found++;

                            log($"[hole-candidate-3edge] add=({outA},{outB},{outC}) areaSq={areaSq:E6} maxEdge={maxEdge:0.000000} normalDot={dot:0.000000}");
                        }
                    }
                }

                log($"[hole-candidates-3edge] count={found}");
            }

            void AnalyzeTwoEdgeWedgeCandidates()
            {
                var emitted = new HashSet<(uint A, uint B, uint C)>();
                var found = 0;

                foreach (var item in boundaryAdj)
                {
                    var m = item.Key;
                    var ns = item.Value;

                    if (ns.Count != 2)
                        continue;

                    var a = ns[0];
                    var c = ns[1];

                    var key = TriangleKey(a, m, c);

                    if (existingTriangles.ContainsKey(key))
                        continue;

                    if (!emitted.Add(key))
                        continue;

                    if (!TryGetCandidateGeometry(a, m, c, out var normal, out var areaSq, out var maxEdge))
                        continue;

                    var expected = GetBoundaryEdgeNormal(a, m) + GetBoundaryEdgeNormal(m, c);
                    var dot = expected.LengthSquared() > normalEps ? Vector3.Dot(normal, Vector3.Normalize(expected)) : 0.0f;

                    var outA = a;
                    var outB = m;
                    var outC = c;

                    if (expected.LengthSquared() > normalEps && dot < 0.0f)
                    {
                        outB = c;
                        outC = m;
                        dot = -dot;
                    }

                    found++;

                    log($"[wedge-candidate-2edge] add=({outA},{outB},{outC}) areaSq={areaSq:E6} maxEdge={maxEdge:0.000000} normalDot={dot:0.000000} weak=true");
                }

                log($"[wedge-candidates-2edge] count={found}");
            }

            void AnalyzeTJunctions()
            {
                if (tJunctionTolerance <= 0.0f)
                {
                    log("[t-junctions] skipped tolerance<=0");
                    return;
                }

                var tolSq = tJunctionTolerance * tJunctionTolerance;
                var found = 0;

                foreach (var edgePair in edges)
                {
                    var a = EdgeA(edgePair.Key);
                    var b = EdgeB(edgePair.Key);
                    var pa = vertexPos[a];
                    var pb = vertexPos[b];

                    foreach (var vertex in vertexPos)
                    {
                        var v = vertex.Key;

                        if (v == a || v == b)
                            continue;

                        var t = SegmentProjection(pa, pb, vertex.Value);

                        if (t <= 0.05f || t >= 0.95f)
                            continue;

                        var closest = pa + (pb - pa) * t;
                        var distSq = Vector3.DistanceSquared(vertex.Value, closest);

                        if (distSq > tolSq)
                            continue;

                        found++;

                        log($"[possible-t-junction] v={v} edge=({a},{b}) t={t:0.000000} dist={MathF.Sqrt(distSq):0.000000} edgeTris={JoinEdgeTriangles(edgePair.Value)}");
                    }
                }

                log($"[t-junctions] count={found}");
            }

            bool TryGetCandidateGeometry(uint a, uint b, uint c, out Vector3 normal, out float areaSq, out float maxEdge)
            {
                var pa = vertexPos[a];
                var pb = vertexPos[b];
                var pc = vertexPos[c];

                var ab = Vector3.Distance(pa, pb);
                var bc = Vector3.Distance(pb, pc);
                var ca = Vector3.Distance(pc, pa);

                maxEdge = MathF.Max(ab, MathF.Max(bc, ca));

                normal = Vector3.Cross(pb - pa, pc - pa);
                areaSq = normal.LengthSquared();

                if (areaSq <= normalEps)
                {
                    normal = Vector3.Zero;
                    return false;
                }

                normal /= MathF.Sqrt(areaSq);
                return true;
            }

            Vector3 GetBoundaryEdgeNormal(uint a, uint b)
            {
                if (!edges.TryGetValue(EdgeKey(a, b), out var list))
                    return Vector3.Zero;

                if (list.Count != 1)
                    return Vector3.Zero;

                return triangles[list[0].Tri].Normal;
            }

            bool IsBoundaryEdge(uint a, uint b)
            {
                return edges.TryGetValue(EdgeKey(a, b), out var list) && list.Count == 1;
            }

            bool ShareEdgeContainingVertex(Triangle a, Triangle b, uint vertex)
            {
                if (!ContainsVertex(a, vertex) || !ContainsVertex(b, vertex))
                    return false;

                var shared = 0;

                if (ContainsVertex(b, a.A))
                    shared++;

                if (ContainsVertex(b, a.B))
                    shared++;

                if (ContainsVertex(b, a.C))
                    shared++;

                return shared >= 2;
            }

            bool ContainsVertex(Triangle tri, uint vertex)
            {
                return tri.A == vertex || tri.B == vertex || tri.C == vertex;
            }

            (uint A, uint B, uint C) TriangleKey(uint a, uint b, uint c)
            {
                if (a > b)
                    (a, b) = (b, a);

                if (b > c)
                    (b, c) = (c, b);

                if (a > b)
                    (a, b) = (b, a);

                return (a, b, c);
            }

            ulong EdgeKey(uint a, uint b)
            {
                if (a > b)
                    (a, b) = (b, a);

                return ((ulong)a << 32) | b;
            }

            uint EdgeA(ulong edge)
            {
                return (uint)(edge >> 32);
            }

            uint EdgeB(ulong edge)
            {
                return (uint)edge;
            }

            float SegmentProjection(Vector3 a, Vector3 b, Vector3 p)
            {
                var ab = b - a;
                var lenSq = ab.LengthSquared();

                if (lenSq <= normalEps)
                    return 0.0f;

                return Vector3.Dot(p - a, ab) / lenSq;
            }

            string TriLabel(int index)
            {
                return $"{index}/id={triangles[index].TriangleId}";
            }

            string JoinTriangles(List<int> ids)
            {
                if (ids.Count == 0)
                    return "";

                var result = TriLabel(ids[0]);

                for (var i = 1; i < ids.Count; i++)
                    result += "," + TriLabel(ids[i]);

                return result;
            }

            string JoinEdgeTriangles(List<(int Tri, uint From, uint To, uint Opposite)> ids)
            {
                if (ids.Count == 0)
                    return "";

                var result = TriLabel(ids[0].Tri);

                for (var i = 1; i < ids.Count; i++)
                    result += "," + TriLabel(ids[i].Tri);

                return result;
            }

            string JoinVertices(List<uint> ids)
            {
                if (ids.Count == 0)
                    return "";

                var result = ids[0].ToString();

                for (var i = 1; i < ids.Count; i++)
                    result += "," + ids[i];

                return result;
            }

            string Fmt(Vector3 v)
            {
                return $"{v.X:0.000000}, {v.Y:0.000000}, {v.Z:0.000000}";
            }
        }

        [Action]
        public async Task PickPoint()
        {
            var pick = Context.Require<IObjectPicker>();

            var collision = await pick.PickAsync(c => c.Object == _host);
            if (collision != null)
            {
                BuildSubMesh((int)collision.TriangleId);
            }
        }

        [Action]
        public void Analyze()
        {
            AnalyzeTrianglePatch(_triangles!.Select(a=> a.Triangle).ToArray(), str => Log.Info(this, str));
        }

        public void Dispose()
        {
            _slice?.Dispose();
            _canvas?.Dispose();
            _canvas= null;
            _slice = null;  
            GC.SuppressFinalize(this);
        }

        public float CellSize { get; set; }

        public float SliceArea { get; set; }

        public bool ShowSubMesh
        {
            get => _showSlice;
            set
            {
                _showSlice = value;
                _host!.IsVisible = !_showSlice;
                _slice?.IsVisible = _showSlice;
            }
        }

    }
}
