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
            List<IList<Vector2>> data = new List<IList<Vector2>>
            {
                outerBoundary
            };

            List<Vector2> allVertices = new List<Vector2>(outerBoundary.Count + (holes?.Count * 4 ?? 0));

            allVertices.AddRange(outerBoundary);

            if (holes != null)
            {
                foreach (IList<Vector2> hole in holes)
                {
                    data.Add(hole);
                    allVertices.AddRange(hole);
                }
            }

            List<int> indices = Mapbox.Earcut.Triangulate(data);

            List<Triangle3> results = new List<Triangle3>(indices.Count / 3);

            for (int i = 0; i < indices.Count; i += 3)
            {
                int i0 = indices[i];
                int i1 = indices[i + 1];
                int i2 = indices[i + 2];

                Vector2 p0 = allVertices[i0];
                Vector2 p1 = allVertices[i1];
                Vector2 p2 = allVertices[i2];

                Triangle3 tri = new Triangle3
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
            List<Triangle3> triangles = new List<Triangle3>();

            // A copy of vertices
            List<Vector2> verts = polygon.ToList();
            List<int> indices = Enumerable.Range(0, verts.Count).ToList();

            while (indices.Count > 3)
            {
                bool earFound = false;

                for (int i = 0; i < indices.Count; i++)
                {
                    int prev = (i - 1 + indices.Count) % indices.Count;
                    int next = (i + 1) % indices.Count;

                    Vector2 A = verts[indices[prev]];
                    Vector2 B = verts[indices[i]];
                    Vector2 C = verts[indices[next]];

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
                Vector2 A = verts[indices[0]];
                Vector2 B = verts[indices[1]];
                Vector2 C = verts[indices[2]];
                triangles.Add(new Triangle3(A.ToVector3(), B.ToVector3(), C.ToVector3()));
            }

            return triangles;
        }

        private static bool IsEar(Vector2 A, Vector2 B, Vector2 C, List<int> indices, List<Vector2> verts)
        {
            if (Area(A, B, C) <= 0)
                return false;

            // Check if any other vertex lies inside the triangle ABC
            for (int j = 0; j < indices.Count; j++)
            {
                Vector2 P = verts[indices[j]];
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
            Vector2 v0 = c - a;
            Vector2 v1 = b - a;
            Vector2 v2 = p - a;

            float dot00 = Vector2.Dot(v0, v0);
            float dot01 = Vector2.Dot(v0, v1);
            float dot02 = Vector2.Dot(v0, v2);
            float dot11 = Vector2.Dot(v1, v1);
            float dot12 = Vector2.Dot(v1, v2);

            float invDenom = 1 / (dot00 * dot11 - dot01 * dot01);
            float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
            float v = (dot00 * dot12 - dot01 * dot02) * invDenom;

            return (u >= 0) && (v >= 0) && (u + v < 1);
        }


        private static float Area(Vector2 a, Vector2 b, Vector2 c)
        {
            return ((b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X)) * 0.5f;
        }

    }
}