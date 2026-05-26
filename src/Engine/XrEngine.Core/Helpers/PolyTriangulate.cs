using System.Numerics;
using XrMath;

namespace XrEngine
{

    public static class PolyTriangulateV2
    {
        public static IList<Triangle3> TriangulateWithHoles(
            IList<Vector2> outerBoundary,
            IList<IList<Vector2>> holes)
        {
            var data = new List<IList<Vector2>>
            {
                outerBoundary
            };

            var allVertices = new List<Vector2>(outerBoundary.Count + (holes?.Count * 4 ?? 0));

            allVertices.AddRange(outerBoundary);

            if (holes != null)
            {
                foreach (var hole in holes)
                {
                    data.Add(hole);
                    allVertices.AddRange(hole);
                }
            }

            var indices = Mapbox.Earcut.Triangulate(data);

            var results = new List<Triangle3>(indices.Count / 3);

            for (var i = 0; i < indices.Count; i += 3)
            {
                var i0 = indices[i];
                var i1 = indices[i + 1];
                var i2 = indices[i + 2];

                var p0 = allVertices[i0];
                var p1 = allVertices[i1];
                var p2 = allVertices[i2];

                var tri = new Triangle3
                {
                    V0 = new Vector3(p0.X, p0.Y, 0),
                    V1 = new Vector3(p1.X, p1.Y, 0),
                    V2 = new Vector3(p2.X, p2.Y, 0)
                };

                results.Add(tri);
            }

            return results;
        }
    }


    public static class PolyTriangulate
    {

        public static List<Triangle3> TriangulateSimplePolygon(IList<Vector2> polygon)
        {
            var triangles = new List<Triangle3>();

            // A copy of vertices
            var verts = polygon.ToList();
            var indices = Enumerable.Range(0, verts.Count).ToList();

            while (indices.Count > 3)
            {
                var earFound = false;

                for (var i = 0; i < indices.Count; i++)
                {
                    var prev = (i - 1 + indices.Count) % indices.Count;
                    var next = (i + 1) % indices.Count;

                    var A = verts[indices[prev]];
                    var B = verts[indices[i]];
                    var C = verts[indices[next]];

                    if (IsEar(A, B, C, indices, verts))
                    {
                        // We found an ear
                        triangles.Add(new Triangle3(A.ToVector3(), B.ToVector3(), C.ToVector3()));
                        indices.RemoveAt(i);
                        earFound = true;
                        break;
                    }
                }

                if (!earFound)
                    break;
            }

            if (indices.Count == 3)
            {
                var A = verts[indices[0]];
                var B = verts[indices[1]];
                var C = verts[indices[2]];
                triangles.Add(new Triangle3(A.ToVector3(), B.ToVector3(), C.ToVector3()));
            }

            return triangles;
        }

        private static bool IsEar(Vector2 A, Vector2 B, Vector2 C, List<int> indices, List<Vector2> verts)
        {
            if (Area(A, B, C) <= 0)
                return false;

            // Check if any other vertex lies inside the triangle ABC
            for (var j = 0; j < indices.Count; j++)
            {
                var P = verts[indices[j]];
                if (P.Equals(A) || P.Equals(B) || P.Equals(C))
                    continue;
                if (PointInTriangle(P, A, B, C))
                    return false;
            }

            return true;
        }

        private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            // Barycentric technique
            var v0 = c - a;
            var v1 = b - a;
            var v2 = p - a;

            var dot00 = Vector2.Dot(v0, v0);
            var dot01 = Vector2.Dot(v0, v1);
            var dot02 = Vector2.Dot(v0, v2);
            var dot11 = Vector2.Dot(v1, v1);
            var dot12 = Vector2.Dot(v1, v2);

            var invDenom = 1 / (dot00 * dot11 - dot01 * dot01);
            var u = (dot11 * dot02 - dot01 * dot12) * invDenom;
            var v = (dot00 * dot12 - dot01 * dot02) * invDenom;

            return (u >= 0) && (v >= 0) && (u + v < 1);
        }


        private static float Area(Vector2 a, Vector2 b, Vector2 c)
        {
            return ((b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X)) * 0.5f;
        }

    }
}