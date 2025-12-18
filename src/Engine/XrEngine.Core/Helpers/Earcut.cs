using System.Numerics;

namespace Mapbox
{
    public static class Earcut
    {

        /// <summary>
        /// Triangulates the given polygon (with optional holes).
        /// </summary>
        /// <param name="points">A list of rings. The first ring is the outer boundary, subsequent rings are holes.</param>
        /// <returns>A list of indices representing triangles (0, 1, 2, 0, 2, 3...).</returns>
        public static List<int> Triangulate(IList<IList<Vector2>> points)
        {
            var earcut = new EarcutProcessor();
            earcut.Process(points);
            return earcut.Indices;
        }

        /// <summary>
        /// Triangulates a flat list of points (single ring).
        /// </summary>
        public static List<int> Triangulate(IList<Vector2> points)
        {
            var earcut = new EarcutProcessor();
            earcut.Process(new List<IList<Vector2>> { points });
            return earcut.Indices;
        }

        // Internal worker class to maintain state during triangulation
        private class EarcutProcessor
        {
            public List<int> Indices = new List<int>();
            private int _vertices = 0;
            private bool _hashing;
            private double _minX, _maxX, _minY, _maxY;
            private double _invSize = 0;

            // Equivalent to the C++ Node struct
            private class Node
            {
                public int i;           // Vertex index
                public double x, y;     // Coordinates
                public Node prev;       // Previous vertex in polygon ring
                public Node next;       // Next vertex in polygon ring
                public int z;           // Z-order curve value
                public Node prevZ;      // Previous node in Z-order
                public Node nextZ;      // Next node in Z-order
                public bool steiner;    // Indicates if this is a steiner point

                public Node(int index, double x, double y)
                {
                    this.i = index;
                    this.x = x;
                    this.y = y;
                    this.steiner = false;
                }
            }

            public void Process(IList<IList<Vector2>> points)
            {
                Indices.Clear();
                _vertices = 0;

                if (points == null || points.Count == 0 || points[0].Count == 0) return;

                int threshold = 80;
                int len = 0;

                for (int i = 0; threshold >= 0 && i < points.Count; i++)
                {
                    threshold -= points[i].Count;
                    len += points[i].Count;
                }

                // In C#, we let the GC handle memory, so we don't pre-allocate a node pool.
                // However, we pre-allocate the indices list.
                Indices.Capacity = len + points[0].Count;

                Node outerNode = LinkedList(points[0], true);

                if (outerNode == null || outerNode.prev == outerNode.next) return;

                if (points.Count > 1)
                {
                    outerNode = EliminateHoles(points, outerNode);
                }

                // If the shape is not too simple, we'll use z-order curve hash later; calculate polygon bbox
                _hashing = threshold < 0;
                if (_hashing)
                {
                    Node p = outerNode.next;
                    _minX = _maxX = outerNode.x;
                    _minY = _maxY = outerNode.y;
                    do
                    {
                        double x = p.x;
                        double y = p.y;
                        if (x < _minX) _minX = x;
                        if (y < _minY) _minY = y;
                        if (x > _maxX) _maxX = x;
                        if (y > _maxY) _maxY = y;
                        p = p.next;
                    } while (p != outerNode);

                    // minX, minY and inv_size are later used to transform coords into integers for z-order calculation
                    _invSize = Math.Max(_maxX - _minX, _maxY - _minY);
                    _invSize = _invSize != 0.0 ? (32767.0 / _invSize) : 0.0;
                }

                EarcutLinked(outerNode);
            }

            // Create a circular doubly linked list from polygon points in the specified winding order
            private Node LinkedList(IList<Vector2> points, bool clockwise)
            {
                double sum = 0;
                int len = points.Count;
                int i, j;
                Node last = null;

                // Calculate original winding order of a polygon ring
                for (i = 0, j = len > 0 ? len - 1 : 0; i < len; j = i++)
                {
                    var p1 = points[i];
                    var p2 = points[j];
                    sum += (p2.X - p1.X) * (p1.Y + p2.Y);
                }

                // Link points into circular doubly-linked list in the specified winding order
                if (clockwise == (sum > 0))
                {
                    for (i = 0; i < len; i++) last = InsertNode(_vertices + i, points[i], last);
                }
                else
                {
                    for (i = len - 1; i >= 0; i--) last = InsertNode(_vertices + i, points[i], last);
                }

                if (last != null && Equals(last, last.next))
                {
                    RemoveNode(last);
                    last = last.next;
                }

                _vertices += len;
                return last;
            }

            // Eliminate colinear or duplicate points
            private Node FilterPoints(Node start, Node end = null)
            {
                if (end == null) end = start;

                Node p = start;
                bool again;
                do
                {
                    again = false;
                    if (!p.steiner && (Equals(p, p.next) || Area(p.prev, p, p.next) == 0))
                    {
                        RemoveNode(p);
                        p = end = p.prev;
                        if (p == p.next) break;
                        again = true;
                    }
                    else
                    {
                        p = p.next;
                    }
                } while (again || p != end);

                return end;
            }

            // Main ear slicing loop which triangulates a polygon (given as a linked list)
            private void EarcutLinked(Node ear, int pass = 0)
            {
                if (ear == null) return;

                // Interlink polygon nodes in z-order
                if (pass == 0 && _hashing) IndexCurve(ear);

                Node stop = ear;
                Node prev, next;

                // Iterate through ears, slicing them one by one
                while (ear.prev != ear.next)
                {
                    prev = ear.prev;
                    next = ear.next;

                    if (_hashing ? IsEarHashed(ear) : IsEar(ear))
                    {
                        // Cut off the triangle
                        Indices.Add(prev.i);
                        Indices.Add(ear.i);
                        Indices.Add(next.i);

                        RemoveNode(ear);

                        // Skipping the next vertex leads to less sliver triangles
                        ear = next.next;
                        stop = next.next;

                        continue;
                    }

                    ear = next;

                    // If we looped through the whole remaining polygon and can't find any more ears
                    if (ear == stop)
                    {
                        // Try filtering points and slicing again
                        if (pass == 0)
                        {
                            EarcutLinked(FilterPoints(ear), 1);
                        }
                        // If this didn't work, try curing all small self-intersections locally
                        else if (pass == 1)
                        {
                            ear = CureLocalIntersections(FilterPoints(ear));
                            EarcutLinked(ear, 2);
                        }
                        // As a last resort, try splitting the remaining polygon into two
                        else if (pass == 2)
                        {
                            SplitEarcut(ear);
                        }

                        break;
                    }
                }
            }

            // Check whether a polygon node forms a valid ear with adjacent nodes
            private bool IsEar(Node ear)
            {
                Node a = ear.prev;
                Node b = ear;
                Node c = ear.next;

                if (Area(a, b, c) >= 0) return false; // reflex, can't be an ear

                // Now make sure we don't have other points inside the potential ear
                Node p = ear.next.next;

                while (p != ear.prev)
                {
                    if (PointInTriangle(a.x, a.y, b.x, b.y, c.x, c.y, p.x, p.y) && Area(p.prev, p, p.next) >= 0)
                    {
                        return false;
                    }
                    p = p.next;
                }

                return true;
            }

            private bool IsEarHashed(Node ear)
            {
                Node a = ear.prev;
                Node b = ear;
                Node c = ear.next;

                if (Area(a, b, c) >= 0) return false; // reflex, can't be an ear

                // Triangle bbox; min & max are calculated like this for speed
                double minTX = Math.Min(a.x, Math.Min(b.x, c.x));
                double minTY = Math.Min(a.y, Math.Min(b.y, c.y));
                double maxTX = Math.Max(a.x, Math.Max(b.x, c.x));
                double maxTY = Math.Max(a.y, Math.Max(b.y, c.y));

                // Z-order range for the current triangle bbox
                int minZ = ZOrder(minTX, minTY);
                int maxZ = ZOrder(maxTX, maxTY);

                Node p = ear.prevZ;
                Node n = ear.nextZ;

                // Look for points inside the triangle in both directions
                while (p != null && p.z >= minZ && n != null && n.z <= maxZ)
                {
                    if (p != ear.prev && p != ear.next &&
                        PointInTriangle(a.x, a.y, b.x, b.y, c.x, c.y, p.x, p.y) &&
                        Area(p.prev, p, p.next) >= 0) return false;
                    p = p.prevZ;

                    if (n != ear.prev && n != ear.next &&
                        PointInTriangle(a.x, a.y, b.x, b.y, c.x, c.y, n.x, n.y) &&
                        Area(n.prev, n, n.next) >= 0) return false;
                    n = n.nextZ;
                }

                // Look for remaining points in decreasing z-order
                while (p != null && p.z >= minZ)
                {
                    if (p != ear.prev && p != ear.next &&
                        PointInTriangle(a.x, a.y, b.x, b.y, c.x, c.y, p.x, p.y) &&
                        Area(p.prev, p, p.next) >= 0) return false;
                    p = p.prevZ;
                }

                // Look for remaining points in increasing z-order
                while (n != null && n.z <= maxZ)
                {
                    if (n != ear.prev && n != ear.next &&
                        PointInTriangle(a.x, a.y, b.x, b.y, c.x, c.y, n.x, n.y) &&
                        Area(n.prev, n, n.next) >= 0) return false;
                    n = n.nextZ;
                }

                return true;
            }

            // Go through all polygon nodes and cure small local self-intersections
            private Node CureLocalIntersections(Node start)
            {
                Node p = start;
                do
                {
                    Node a = p.prev;
                    Node b = p.next.next;

                    if (!Equals(a, b) && Intersects(a, p, p.next, b) && LocallyInside(a, b) && LocallyInside(b, a))
                    {
                        Indices.Add(a.i);
                        Indices.Add(p.i);
                        Indices.Add(b.i);

                        RemoveNode(p);
                        RemoveNode(p.next);

                        p = start = b;
                    }
                    p = p.next;
                } while (p != start);

                return FilterPoints(p);
            }

            // Try splitting polygon into two and triangulate them independently
            private void SplitEarcut(Node start)
            {
                Node a = start;
                do
                {
                    Node b = a.next.next;
                    while (b != a.prev)
                    {
                        if (a.i != b.i && IsValidDiagonal(a, b))
                        {
                            Node c = SplitPolygon(a, b);
                            a = FilterPoints(a, a.next);
                            c = FilterPoints(c, c.next);
                            EarcutLinked(a);
                            EarcutLinked(c);
                            return;
                        }
                        b = b.next;
                    }
                    a = a.next;
                } while (a != start);
            }

            // Link every hole into the outer loop, producing a single-ring polygon without holes
            private Node EliminateHoles(IList<IList<Vector2>> points, Node outerNode)
            {
                var queue = new List<Node>();
                int len = points.Count;

                for (int i = 1; i < len; i++)
                {
                    Node list = LinkedList(points[i], false);
                    if (list != null)
                    {
                        if (list == list.next) list.steiner = true;
                        queue.Add(GetLeftmost(list));
                    }
                }

                queue.Sort((a, b) => a.x.CompareTo(b.x));

                // Process holes from left to right
                for (int i = 0; i < queue.Count; i++)
                {
                    outerNode = EliminateHole(queue[i], outerNode);
                }

                return outerNode;
            }

            // Find a bridge between vertices that connects hole with an outer ring and link it
            private Node EliminateHole(Node hole, Node outerNode)
            {
                Node bridge = FindHoleBridge(hole, outerNode);
                if (bridge == null)
                {
                    return outerNode;
                }

                Node bridgeReverse = SplitPolygon(bridge, hole);

                // Filter collinear points around the cuts
                Node n = FilterPoints(bridgeReverse, bridgeReverse.next);
                FilterPoints(bridge, bridge.next);

                return outerNode;
            }

            // David Eberly's algorithm for finding a bridge between hole and outer polygon
            private Node FindHoleBridge(Node hole, Node outerNode)
            {
                Node p = outerNode;
                double hx = hole.x;
                double hy = hole.y;
                double qx = double.NegativeInfinity;
                Node m = null;

                // Find a segment intersected by a ray from the hole's leftmost vertex to the left;
                // segment's endpoint with lesser x will be potential connection vertex
                do
                {
                    if (hy <= p.y && hy >= p.next.y && p.next.y != p.y)
                    {
                        double x = p.x + (hy - p.y) * (p.next.x - p.x) / (p.next.y - p.y);
                        if (x <= hx && x > qx)
                        {
                            qx = x;
                            m = p.x < p.next.x ? p : p.next;
                            if (x == hx) return m;
                        }
                    }
                    p = p.next;
                } while (p != outerNode);

                if (m == null) return null;

                Node stop = m;
                double tanMin = double.PositiveInfinity;
                double tanCur = 0;

                p = m;
                double mx = m.x;
                double my = m.y;

                do
                {
                    if (hx >= p.x && p.x >= mx && hx != p.x &&
                        PointInTriangle(hy < my ? hx : qx, hy, mx, my, hy < my ? qx : hx, hy, p.x, p.y))
                    {
                        tanCur = Math.Abs(hy - p.y) / (hx - p.x); // tangential

                        if (LocallyInside(p, hole) &&
                            (tanCur < tanMin || (tanCur == tanMin && (p.x > m.x || SectorContainsSector(m, p)))))
                        {
                            m = p;
                            tanMin = tanCur;
                        }
                    }

                    p = p.next;
                } while (p != stop);

                return m;
            }

            private bool SectorContainsSector(Node m, Node p)
            {
                return Area(m.prev, m, p.prev) < 0 && Area(p.next, m, m.next) < 0;
            }

            // Interlink polygon nodes in z-order
            private void IndexCurve(Node start)
            {
                Node p = start;
                do
                {
                    if (p.z == 0) p.z = ZOrder(p.x, p.y);
                    p.prevZ = p.prev;
                    p.nextZ = p.next;
                    p = p.next;
                } while (p != start);

                p.prevZ.nextZ = null;
                p.prevZ = null;

                SortLinked(p);
            }

            // Simon Tatham's linked list merge sort algorithm
            private Node SortLinked(Node list)
            {
                Node p, q, e, tail;
                int i, numMerges, pSize, qSize;
                int inSize = 1;

                do
                {
                    p = list;
                    list = null;
                    tail = null;
                    numMerges = 0;

                    while (p != null)
                    {
                        numMerges++;
                        q = p;
                        pSize = 0;
                        for (i = 0; i < inSize; i++)
                        {
                            pSize++;
                            q = q.nextZ;
                            if (q == null) break;
                        }

                        qSize = inSize;

                        while (pSize > 0 || (qSize > 0 && q != null))
                        {
                            if (pSize == 0)
                            {
                                e = q;
                                q = q.nextZ;
                                qSize--;
                            }
                            else if (qSize == 0 || q == null)
                            {
                                e = p;
                                p = p.nextZ;
                                pSize--;
                            }
                            else if (p.z <= q.z)
                            {
                                e = p;
                                p = p.nextZ;
                                pSize--;
                            }
                            else
                            {
                                e = q;
                                q = q.nextZ;
                                qSize--;
                            }

                            if (tail != null) tail.nextZ = e;
                            else list = e;

                            e.prevZ = tail;
                            tail = e;
                        }

                        p = q;
                    }

                    if (tail != null) tail.nextZ = null;
                    inSize *= 2;

                } while (numMerges > 1);

                return list;
            }

            // Z-order of a vertex given coords and size of the data bounding box
            private int ZOrder(double x, double y)
            {
                // Coords are transformed into non-negative 15-bit integer range
                int ix = (int)((x - _minX) * _invSize);
                int iy = (int)((y - _minY) * _invSize);

                ix = (ix | (ix << 8)) & 0x00FF00FF;
                ix = (ix | (ix << 4)) & 0x0F0F0F0F;
                ix = (ix | (ix << 2)) & 0x33333333;
                ix = (ix | (ix << 1)) & 0x55555555;

                iy = (iy | (iy << 8)) & 0x00FF00FF;
                iy = (iy | (iy << 4)) & 0x0F0F0F0F;
                iy = (iy | (iy << 2)) & 0x33333333;
                iy = (iy | (iy << 1)) & 0x55555555;

                return ix | (iy << 1);
            }

            // Find the leftmost node of a polygon ring
            private Node GetLeftmost(Node start)
            {
                Node p = start;
                Node leftmost = start;
                do
                {
                    if (p.x < leftmost.x || (p.x == leftmost.x && p.y < leftmost.y)) leftmost = p;
                    p = p.next;
                } while (p != start);

                return leftmost;
            }

            // Check if a point lies within a convex triangle
            private bool PointInTriangle(double ax, double ay, double bx, double by, double cx, double cy, double px, double py)
            {
                return (cx - px) * (ay - py) >= (ax - px) * (cy - py) &&
                       (ax - px) * (by - py) >= (bx - px) * (ay - py) &&
                       (bx - px) * (cy - py) >= (cx - px) * (by - py);
            }

            // Check if a diagonal between two polygon nodes is valid (lies in polygon interior)
            private bool IsValidDiagonal(Node a, Node b)
            {
                return a.next.i != b.i && a.prev.i != b.i && !IntersectsPolygon(a, b) &&
                       ((LocallyInside(a, b) && LocallyInside(b, a) && MiddleInside(a, b) &&
                         (Area(a.prev, a, b.prev) != 0 || Area(a, b.prev, b) != 0)) ||
                        (Equals(a, b) && Area(a.prev, a, a.next) > 0 && Area(b.prev, b, b.next) > 0));
            }

            // Signed area of a triangle
            private double Area(Node p, Node q, Node r)
            {
                return (q.y - p.y) * (r.x - q.x) - (q.x - p.x) * (r.y - q.y);
            }

            private bool Equals(Node p1, Node p2)
            {
                return p1.x == p2.x && p1.y == p2.y;
            }

            // Check if two segments intersect
            private bool Intersects(Node p1, Node q1, Node p2, Node q2)
            {
                int o1 = Sign(Area(p1, q1, p2));
                int o2 = Sign(Area(p1, q1, q2));
                int o3 = Sign(Area(p2, q2, p1));
                int o4 = Sign(Area(p2, q2, q1));

                if (o1 != o2 && o3 != o4) return true; // General case

                if (o1 == 0 && OnSegment(p1, p2, q1)) return true;
                if (o2 == 0 && OnSegment(p1, q2, q1)) return true;
                if (o3 == 0 && OnSegment(p2, p1, q2)) return true;
                if (o4 == 0 && OnSegment(p2, q1, q2)) return true;

                return false;
            }

            private bool OnSegment(Node p, Node q, Node r)
            {
                return q.x <= Math.Max(p.x, r.x) && q.x >= Math.Min(p.x, r.x) &&
                       q.y <= Math.Max(p.y, r.y) && q.y >= Math.Min(p.y, r.y);
            }

            private int Sign(double val)
            {
                return (0 < val ? 1 : 0) - (val < 0 ? 1 : 0);
            }

            private bool IntersectsPolygon(Node a, Node b)
            {
                Node p = a;
                do
                {
                    if (p.i != a.i && p.next.i != a.i && p.i != b.i && p.next.i != b.i &&
                        Intersects(p, p.next, a, b)) return true;
                    p = p.next;
                } while (p != a);

                return false;
            }

            private bool LocallyInside(Node a, Node b)
            {
                return Area(a.prev, a, a.next) < 0
                    ? Area(a, b, a.next) >= 0 && Area(a, a.prev, b) >= 0
                    : Area(a, b, a.prev) < 0 || Area(a, a.next, b) < 0;
            }

            private bool MiddleInside(Node a, Node b)
            {
                Node p = a;
                bool inside = false;
                double px = (a.x + b.x) / 2;
                double py = (a.y + b.y) / 2;
                do
                {
                    if (((p.y > py) != (p.next.y > py)) && p.next.y != p.y &&
                        (px < (p.next.x - p.x) * (py - p.y) / (p.next.y - p.y) + p.x))
                        inside = !inside;
                    p = p.next;
                } while (p != a);

                return inside;
            }

            private Node SplitPolygon(Node a, Node b)
            {
                Node a2 = new Node(a.i, a.x, a.y);
                Node b2 = new Node(b.i, b.x, b.y);
                Node an = a.next;
                Node bp = b.prev;

                a.next = b;
                b.prev = a;

                a2.next = an;
                an.prev = a2;

                b2.next = a2;
                a2.prev = b2;

                bp.next = b2;
                b2.prev = bp;

                return b2;
            }

            private Node InsertNode(int i, Vector2 pt, Node last)
            {
                Node p = new Node(i, pt.X, pt.Y);

                if (last == null)
                {
                    p.prev = p;
                    p.next = p;
                }
                else
                {
                    p.next = last.next;
                    p.prev = last;
                    last.next.prev = p;
                    last.next = p;
                }
                return p;
            }

            private void RemoveNode(Node p)
            {
                p.next.prev = p.prev;
                p.prev.next = p.next;

                if (p.prevZ != null) p.prevZ.nextZ = p.nextZ;
                if (p.nextZ != null) p.nextZ.prevZ = p.prevZ;
            }
        }
    }
}