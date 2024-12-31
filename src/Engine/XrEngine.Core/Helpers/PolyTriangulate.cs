using System.Numerics;
using XrMath;

namespace XrEngine
{
    public static class PolyTriangulate
    {
        public static List<Triangle3> TriangulateWithHoles(
              IList<Vector2> outerBoundary,
              IList<IList<Vector2>> holes)
        {
            // 1. Ensure orientation of outer boundary is CCW
            if (!IsCounterClockwise(outerBoundary))
            {
                var reversed = outerBoundary.Reverse().ToList();
                outerBoundary = reversed;
            }

            // 2. Ensure each hole is CW
            for (int i = 0; i < holes.Count; i++)
            {
                if (IsCounterClockwise(holes[i]))
                {
                    holes[i] = holes[i].Reverse().ToList();
                }
            }

            // 3. Merge holes into outer boundary by adding "bridge" edges.
            // This is a conceptual approach: we pick a vertex from each hole and connect it
            // to a suitable vertex on the outer polygon. For simplicity, we pick the leftmost
            // vertex of the hole and connect it to a vertex on the outer polygon that is horizontally closest.
            // A more robust approach would involve using a proper polygon clipping library.
            List<Vector2> mergedPolygon = outerBoundary.ToList();

            foreach (var hole in holes)
            {
                // Find leftmost vertex in hole
                var holeLeftmost = hole.Aggregate((curMin, v) => (v.X < curMin.X) ? v : curMin);

                // Find a vertex on outer boundary to connect to. For simplicity,
                // choose the vertex on the outer boundary that is closest horizontally to the holeLeftmost.
                Vector2 bestOuter = mergedPolygon[0];
                float bestDist = float.MaxValue;
                foreach (var v in mergedPolygon)
                {
                    float dist = Math.Abs(v.X - holeLeftmost.X);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestOuter = v;
                    }
                }

                // Create a connection: bestOuter -> holeLeftmost
                // Insert the hole polygon into the merged polygon by cutting at bestOuter
                // and weaving in the hole + these two bridge edges.

                // Find index of bestOuter
                int idx = mergedPolygon.IndexOf(bestOuter);
                if (idx < 0) { throw new Exception("Internal error: bestOuter not found."); }

                // The merged polygon: outer[0..idx], hole vertices, outer[idx..end]
                // with a bridge: bestOuter->holeLeftmost and holeLeftmost->bestOuter.
                // Actually, to keep polygon simple, we do something like:
                // Insert the hole polygon in reverse order (except the chosen vertex),
                // forming a "detour" that includes the hole.

                // We'll connect bestOuter to holeLeftmost and then proceed around the hole, 
                // and then back to bestOuter.

                List<Vector2> newPolygon = new List<Vector2>();
                // Keep outer from start to idx:
                for (int iO = 0; iO <= idx; iO++)
                    newPolygon.Add(mergedPolygon[iO]);

                // Add bridge from bestOuter to holeLeftmost
                newPolygon.Add(holeLeftmost);

                // Add the hole polygon (note: hole is in CW order, we want to move along it
                // so that it remains inside. We might simply follow hole order as is.)
                // Start from holeLeftmost index to the end and then from start to holeLeftmost index:
                int hlIdx = hole.IndexOf(holeLeftmost);
                for (int h_i = 1; h_i < hole.Count; h_i++)
                {
                    int h_idx = (hlIdx + h_i) % hole.Count;
                    newPolygon.Add(hole[h_idx]);
                }

                // Add bridge from last hole vertex back to bestOuter
                newPolygon.Add(bestOuter);

                // Now continue outer from idx+1 to the end
                for (int iO = idx + 1; iO < mergedPolygon.Count; iO++)
                    newPolygon.Add(mergedPolygon[iO]);

                mergedPolygon = newPolygon;
            }

            // Now we have a single polygon without holes (but possibly with extra edges).
            // 4. Triangulate this merged polygon.
            return TriangulateSimplePolygon(mergedPolygon);
        }

        /// <summary>
        /// Triangulate a simple polygon (no holes) using ear clipping.
        /// Polygon is assumed to be in CCW order and simple (no self-intersections).
        /// </summary>
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
                {
                    // No ear found, polygon might be invalid or have colinear points causing issues.
                    // Implement a fallback or debugging here.
                    break;
                }
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

        private static float Area(IList<Vector2> polygon)
        {
            float area = 0;
            for (int i = 0; i < polygon.Count; i++)
            {
                int j = (i + 1) % polygon.Count;
                area += polygon[i].X * polygon[j].Y - polygon[j].X * polygon[i].Y;
            }
            return area * 0.5f;
        }

        private static float Area(Vector2 a, Vector2 b, Vector2 c)
        {
            return ((b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X)) * 0.5f;
        }

        private static bool IsCounterClockwise(IList<Vector2> polygon)
        {
            return Area(polygon) > 0;
        }
    }
}