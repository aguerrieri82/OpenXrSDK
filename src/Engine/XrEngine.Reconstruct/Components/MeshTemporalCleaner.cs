using System;
using System.Collections.Generic;
using System.Numerics;
using XrEngine;
using XrEngine.Reconstruct;
using XrMath;

public enum ProbeMode
{
    Normal,
    CameraRay,
    NormalOrCameraRay
}

public class TemporalGridCleaner
{
    private const float Eps = 0.00000001f;

    public static  void BuildCleanVertices(
        IReadOnlyList<TriangleMesh> meshes,
        float distance,
        ProbeMode probeMode = ProbeMode.Normal,
        float cellSize = 0.0f)
    {
        if (meshes.Count == 0)
            return;

        if (distance <= 0.0f)
            throw new ArgumentOutOfRangeException(nameof(distance));

        if (cellSize <= 0.0f)
            cellSize = distance * 2.0f;

        var invCell = 1.0f / cellSize;
        var maxRayDistance = distance * 2.0f;

        var flat = new FlatMesh[meshes.Count];

        for (var i = 0; i < meshes.Count; i++)
        {
            Log.Info(typeof(TemporalGridCleaner), "Build flast mesh {0}", i);

            flat[i] = BuildFlatMesh(meshes[i]);
            BuildTriangleHash(flat[i], invCell);
        }

        for (var newIndex = flat.Length - 1; newIndex >= 1; newIndex--)
        {
            Log.Info(typeof(TemporalGridCleaner), "Cleaning {0}", newIndex);

            var newer = flat[newIndex];

            for (var oldIndex = newIndex - 1; oldIndex >= 0; oldIndex--)
            {
                var older = flat[oldIndex];

                Log.Debug(typeof(TemporalGridCleaner), "Probing {0}", oldIndex);

                if (!IntersectsInflated(newer.Bounds, older.Bounds, distance))
                {
                    Log.Debug(typeof(TemporalGridCleaner), "Skipped {0}", oldIndex);
                    continue;
                }

                KillOlderTriangles(
                    newer,
                    older,
                    distance,
                    maxRayDistance,
                    invCell,
                    probeMode);
            }
        }

        for (var m = 0; m < flat.Length; m++)
        {
            Log.Info(typeof(TemporalGridCleaner), "Sparse cleanup {0}", m);

            KillSparseTriangles(
                flat[m].Triangles,
                meshIndex: m,
                quantSize: 0.002f,
                iterations: 3,
                minSharedEdges: 3);

            Log.Info(typeof(TemporalGridCleaner), "Rebuild {0}", m);

            var tris = flat[m].Triangles;

            var vertices = new List<VertexData>();

            for (var i = 0; i < tris.Length; i++)
            {
                if (!tris[i].Alive)
                    continue;

                vertices.Add(new VertexData { Pos = tris[i].P0, UV = tris[i].UV0 });
                vertices.Add(new VertexData { Pos = tris[i].P1, UV = tris[i].UV1 });
                vertices.Add(new VertexData { Pos = tris[i].P2, UV = tris[i].UV2 });
            }

            Log.Info(
                typeof(TemporalGridCleaner),
                "Rebuild {0}: vertices={1}, triangles={2}",
                m,
                vertices.Count,
                vertices.Count / 3);

            meshes[m].Geometry!.Vertices = vertices.ToArray();
            //meshes[m].Geometry!.ComputeNormals();
            meshes[m].Geometry!.ComputeIndices();
        }
    }

    private static unsafe FlatMesh BuildFlatMesh(TriangleMesh mesh, float border = (1 / 300f) * 20)
    {
        var geo = mesh.Geometry;

        geo.SmoothNormals();

        var vertices = geo.Vertices;
        var indices = geo.Indices;

        var triCount = indices.Length > 0
            ? indices.Length / 3
            : vertices.Length / 3;

        var tris = new WorkTri[triCount];
        var normalSum = new Vector3[vertices.Length];

        var captureView = mesh.Component<CaptureFrame>()!.Meta!.CameraView;
        var captureCameraPos = captureView.Invert().Translation;

        fixed (VertexData* vPtr = vertices)
        fixed (uint* iPtr = indices)
        {
            if (indices.Length > 0)
            {
                var ti = 0;

                for (var i = 0; i < indices.Length;)
                {
                    var i0 = iPtr[i++];
                    var i1 = iPtr[i++];
                    var i2 = iPtr[i++];

                    var v0 = vPtr[i0];
                    var v1 = vPtr[i1];
                    var v2 = vPtr[i2];

                    var tri = CreateTri(
                        v0.Pos, v1.Pos, v2.Pos,
                        v0.UV, v1.UV, v2.UV);

                    var n = Vector3.Cross(tri.P1 - tri.P0, tri.P2 - tri.P0);
                    var nLenSq = n.LengthSquared();

                    if (nLenSq <= Eps)
                    {
                        tri.Alive = false;
                    }
                    else
                    {
                        normalSum[i0] += n;
                        normalSum[i1] += n;
                        normalSum[i2] += n;
                    }

                    tris[ti++] = tri;
                }
            }
            else
            {
                var ti = 0;

                for (uint i = 0; i < vertices.Length;)
                {
                    var i0 = i++;
                    var i1 = i++;
                    var i2 = i++;

                    var v0 = vPtr[i0];
                    var v1 = vPtr[i1];
                    var v2 = vPtr[i2];

                    var tri = CreateTri(
                        v0.Pos, v1.Pos, v2.Pos,
                        v0.UV, v1.UV, v2.UV);

                    var n = Vector3.Cross(tri.P1 - tri.P0, tri.P2 - tri.P0);
                    var nLenSq = n.LengthSquared();

                    if (nLenSq <= Eps)
                    {
                        tri.Alive = false;
                    }
                    else
                    {
                        normalSum[i0] += n;
                        normalSum[i1] += n;
                        normalSum[i2] += n;
                    }

                    tris[ti++] = tri;
                }
            }

            var probes = new Probe[vertices.Length];
         
            int probesAdded = 0;

            for (var i = 0; i < vertices.Length; i++)
            {
                var p = vPtr[i].Pos;

                var uv = vPtr[i].UV;
                if (uv.X < border || uv.Y < border || uv.X > 1 - border || uv.Y > 1 - border)
                    continue;

                var cameraDir = SafeNormalize(p - captureCameraPos);

                var normal = normalSum[i];

                if (normal.LengthSquared() <= Eps)
                    normal = vPtr[i].Normal;

                normal = SafeNormalize(normal);

                if (normal.LengthSquared() <= Eps)
                    normal = cameraDir;

                probes[probesAdded++] = new Probe
                {
                    Pos = p,
                    Normal = normal,
                    CameraDir = cameraDir
                };
            }

            Array.Resize(ref probes, probesAdded);

            return new FlatMesh
            {
                Triangles = tris,
                Probes = probes,
                Bounds = ToBounds(mesh.WorldBounds),
                Hash = []
            };
        }
    }

    private static WorkTri CreateTri(
        Vector3 p0,
        Vector3 p1,
        Vector3 p2,
        Vector2 uv0,
        Vector2 uv1,
        Vector2 uv2)
    {
        return new WorkTri
        {
            P0 = p0,
            P1 = p1,
            P2 = p2,

            UV0 = uv0,
            UV1 = uv1,
            UV2 = uv2,

            Bounds = new WorkBounds(
                Min(Min(p0, p1), p2),
                Max(Max(p0, p1), p2)),

            Alive = true
        };
    }

    private static void BuildTriangleHash(FlatMesh mesh, float invCell)
    {
        var tris = mesh.Triangles;
        var hash = mesh.Hash;

        for (var i = 0; i < tris.Length; i++)
        {
            if (!tris[i].Alive)
                continue;

            InsertTriangle(hash, i, tris[i].Bounds, invCell);
        }
    }

    private static void KillOlderTriangles(
        FlatMesh newer,
        FlatMesh older,
        float distance,
        float maxRayDistance,
        float invCell,
        ProbeMode probeMode)
    {
        var visited = new int[older.Triangles.Length];
        var stamp = 1;

        var probes = newer.Probes;

        for (var i = 0; i < probes.Length; i++)
        {
            var probe = probes[i];

            if (probeMode == ProbeMode.Normal)
            {
                TestProbeDirection(
                    older,
                    probe.Pos,
                    probe.Normal,
                    distance,
                    maxRayDistance,
                    invCell,
                    visited,
                    ref stamp);
            }
            else if (probeMode == ProbeMode.CameraRay)
            {
                TestProbeDirection(
                    older,
                    probe.Pos,
                    probe.CameraDir,
                    distance,
                    maxRayDistance,
                    invCell,
                    visited,
                    ref stamp);
            }
            else
            {
                TestProbeDirection(
                    older,
                    probe.Pos,
                    probe.Normal,
                    distance,
                    maxRayDistance,
                    invCell,
                    visited,
                    ref stamp);

                TestProbeDirection(
                    older,
                    probe.Pos,
                    probe.CameraDir,
                    distance,
                    maxRayDistance,
                    invCell,
                    visited,
                    ref stamp);
            }
        }
    }

    private static void TestProbeDirection(
        FlatMesh older,
        Vector3 pos,
        Vector3 dir,
        float distance,
        float maxRayDistance,
        float invCell,
        int[] visited,
        ref int stamp)
    {
        if (dir.LengthSquared() <= Eps)
            return;

        var start = pos - dir * distance;
        var end = pos + dir * distance;

        var probeBounds = new WorkBounds(
            Min(start, end),
            Max(start, end));

        var minCell = ToCell(probeBounds.Min, invCell);
        var maxCell = ToCell(probeBounds.Max, invCell);

        var ray = new Ray3(start, dir);

        stamp++;

        if (stamp == int.MaxValue)
        {
            Array.Clear(visited);
            stamp = 1;
        }

        var tris = older.Triangles;
        var hash = older.Hash;

        for (var z = minCell.Z; z <= maxCell.Z; z++)
            for (var y = minCell.Y; y <= maxCell.Y; y++)
                for (var x = minCell.X; x <= maxCell.X; x++)
                {
                    if (!hash.TryGetValue(new CellKey(x, y, z), out var ids))
                        continue;

                    for (var i = 0; i < ids.Count; i++)
                    {
                        var triIndex = ids[i];

                        if (visited[triIndex] == stamp)
                            continue;

                        visited[triIndex] = stamp;

                        ref var tri = ref tris[triIndex];

                        if (!tri.Alive)
                            continue;

                        if (!Intersects(tri.Bounds, probeBounds))
                            continue;

                        var triangle = new Triangle3
                        {
                            V0 = tri.P0,
                            V1 = tri.P1,
                            V2 = tri.P2
                        };

                        if (ray.Intersects(triangle, out var hitDistance) != null &&
                            hitDistance <= maxRayDistance)
                        {
                            tri.Alive = false;
                        }
                    }
                }
    }

    private static void InsertTriangle(
        Dictionary<CellKey, List<int>> hash,
        int triIndex,
        WorkBounds bounds,
        float invCell)
    {
        var minCell = ToCell(bounds.Min, invCell);
        var maxCell = ToCell(bounds.Max, invCell);

        for (var z = minCell.Z; z <= maxCell.Z; z++)
            for (var y = minCell.Y; y <= maxCell.Y; y++)
                for (var x = minCell.X; x <= maxCell.X; x++)
                {
                    var key = new CellKey(x, y, z);

                    if (!hash.TryGetValue(key, out var list))
                    {
                        list = [];
                        hash[key] = list;
                    }

                    list.Add(triIndex);
                }
    }

    private static void KillSparseTriangles(
        WorkTri[] tris,
        int meshIndex,
        float quantSize,
        int iterations,
        int minSharedEdges)
    {
        if (tris.Length == 0)
            return;

        if (quantSize <= 0.0f)
            throw new ArgumentOutOfRangeException(nameof(quantSize));

        if (iterations <= 0)
            return;

        var aliveStart = CountAlive(tris);
        var totalKilled = 0;
        var invQuant = 1.0f / quantSize;
        var kill = new bool[tris.Length];

        for (var iter = 0; iter < iterations; iter++)
        {
            Array.Clear(kill);

            var edgeCounts = new Dictionary<EdgeKey, int>(tris.Length * 3);

            for (var i = 0; i < tris.Length; i++)
            {
                if (!tris[i].Alive)
                    continue;

                AddEdge(edgeCounts, tris[i].P0, tris[i].P1, invQuant);
                AddEdge(edgeCounts, tris[i].P1, tris[i].P2, invQuant);
                AddEdge(edgeCounts, tris[i].P2, tris[i].P0, invQuant);
            }

            var killed = 0;
            var aliveBeforeIter = 0;

            for (var i = 0; i < tris.Length; i++)
            {
                if (!tris[i].Alive)
                    continue;

                aliveBeforeIter++;

                var sharedEdges = 0;

                if (GetEdgeCount(edgeCounts, tris[i].P0, tris[i].P1, invQuant) > 1)
                    sharedEdges++;

                if (GetEdgeCount(edgeCounts, tris[i].P1, tris[i].P2, invQuant) > 1)
                    sharedEdges++;

                if (GetEdgeCount(edgeCounts, tris[i].P2, tris[i].P0, invQuant) > 1)
                    sharedEdges++;

                if (sharedEdges < minSharedEdges)
                {
                    kill[i] = true;
                    killed++;
                }
            }

            if (killed == 0)
                break;

            for (var i = 0; i < tris.Length; i++)
            {
                if (kill[i])
                    tris[i].Alive = false;
            }

            totalKilled += killed;

        }

        Log.Debug(
            typeof(TemporalGridCleaner),
            "Sparse {0}: done killed={1}, aliveBefore={2}, aliveAfter={3}",
            meshIndex,
            totalKilled,
            aliveStart,
            aliveStart - totalKilled);
    }

    private static WorkBounds ToBounds(Bounds3 b)
    {
        return new WorkBounds(b.Min, b.Max);
    }

    private static bool IntersectsInflated(WorkBounds a, WorkBounds b, float inflate)
    {
        return
            a.Min.X - inflate <= b.Max.X && a.Max.X + inflate >= b.Min.X &&
            a.Min.Y - inflate <= b.Max.Y && a.Max.Y + inflate >= b.Min.Y &&
            a.Min.Z - inflate <= b.Max.Z && a.Max.Z + inflate >= b.Min.Z;
    }

    private static bool Intersects(WorkBounds a, WorkBounds b)
    {
        return
            a.Min.X <= b.Max.X && a.Max.X >= b.Min.X &&
            a.Min.Y <= b.Max.Y && a.Max.Y >= b.Min.Y &&
            a.Min.Z <= b.Max.Z && a.Max.Z >= b.Min.Z;
    }

    private static CellKey ToCell(Vector3 p, float invCell)
    {
        return new CellKey(
            (int)MathF.Floor(p.X * invCell),
            (int)MathF.Floor(p.Y * invCell),
            (int)MathF.Floor(p.Z * invCell));
    }

    private static Vector3 SafeNormalize(Vector3 v)
    {
        var lenSq = v.LengthSquared();

        if (lenSq <= Eps)
            return Vector3.Zero;

        return v / MathF.Sqrt(lenSq);
    }

    private static Vector3 Min(Vector3 a, Vector3 b)
    {
        return new Vector3(
            MathF.Min(a.X, b.X),
            MathF.Min(a.Y, b.Y),
            MathF.Min(a.Z, b.Z));
    }

    private static Vector3 Max(Vector3 a, Vector3 b)
    {
        return new Vector3(
            MathF.Max(a.X, b.X),
            MathF.Max(a.Y, b.Y),
            MathF.Max(a.Z, b.Z));
    }

    private sealed class FlatMesh
    {
        public WorkTri[] Triangles = [];
        public Probe[] Probes = [];
        public WorkBounds Bounds;
        public Dictionary<CellKey, List<int>> Hash = [];
    }

    private struct WorkTri
    {
        public Vector3 P0;
        public Vector3 P1;
        public Vector3 P2;

        public Vector2 UV0;
        public Vector2 UV1;
        public Vector2 UV2;

        public WorkBounds Bounds;
        public bool Alive;
    }

    private struct Probe
    {
        public Vector3 Pos;
        public Vector3 Normal;
        public Vector3 CameraDir;
    }

    private readonly struct WorkBounds
    {
        public readonly Vector3 Min;
        public readonly Vector3 Max;

        public WorkBounds(Vector3 min, Vector3 max)
        {
            Min = min;
            Max = max;
        }
    }

    private readonly struct QuantizedPoint : IEquatable<QuantizedPoint>, IComparable<QuantizedPoint>
    {
        public readonly int X;
        public readonly int Y;
        public readonly int Z;

        public QuantizedPoint(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public int CompareTo(QuantizedPoint other)
        {
            var c = X.CompareTo(other.X);
            if (c != 0)
                return c;

            c = Y.CompareTo(other.Y);
            if (c != 0)
                return c;

            return Z.CompareTo(other.Z);
        }

        public bool Equals(QuantizedPoint other)
        {
            return X == other.X && Y == other.Y && Z == other.Z;
        }

        public override bool Equals(object? obj)
        {
            return obj is QuantizedPoint other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Z);
        }
    }

    private readonly struct EdgeKey : IEquatable<EdgeKey>
    {
        public readonly QuantizedPoint A;
        public readonly QuantizedPoint B;

        public EdgeKey(QuantizedPoint a, QuantizedPoint b)
        {
            if (a.CompareTo(b) <= 0)
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
            return A.Equals(other.A) && B.Equals(other.B);
        }

        public override bool Equals(object? obj)
        {
            return obj is EdgeKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(A, B);
        }
    }

    private static int CountAlive(WorkTri[] tris)
    {
        var count = 0;

        for (var i = 0; i < tris.Length; i++)
        {
            if (tris[i].Alive)
                count++;
        }

        return count;
    }

    private static void AddEdge(
        Dictionary<EdgeKey, int> edgeCounts,
        Vector3 a,
        Vector3 b,
        float invQuant)
    {
        var key = new EdgeKey(
            Quantize(a, invQuant),
            Quantize(b, invQuant));

        edgeCounts.TryGetValue(key, out var count);
        edgeCounts[key] = count + 1;
    }

    private static int GetEdgeCount(
        Dictionary<EdgeKey, int> edgeCounts,
        Vector3 a,
        Vector3 b,
        float invQuant)
    {
        var key = new EdgeKey(
            Quantize(a, invQuant),
            Quantize(b, invQuant));

        return edgeCounts.TryGetValue(key, out var count)
            ? count
            : 0;
    }

    private static QuantizedPoint Quantize(Vector3 p, float invQuant)
    {
        return new QuantizedPoint(
            (int)MathF.Round(p.X * invQuant),
            (int)MathF.Round(p.Y * invQuant),
            (int)MathF.Round(p.Z * invQuant));
    }

    private readonly struct CellKey : IEquatable<CellKey>
    {
        public readonly int X;
        public readonly int Y;
        public readonly int Z;

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
    }
}