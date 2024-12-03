using System.Globalization;
using System.Numerics;

namespace XrMath
{
    public struct Path2Point
    {
        public Path2Segment Segment;
        public float Time;
        public Vector2 Point;
    }

    public class Path2Segment
    {
        protected long _version;
        protected float? _length;
        protected Bounds2? _bounds;

        private float QuadraticLength()
        {
            // Compute the coefficients
            var A = P2 - 2 * P2 + P1;
            var B = 2 * (P2 - P1);

            float a = Vector2.Dot(A, A);
            float b = 2 * Vector2.Dot(A, B);
            float c = Vector2.Dot(B, B);

            // Define helper for the integral
            float s = MathF.Sqrt(4 * a * c - b * b);
            float term1 = MathF.Sqrt(a + b + c) / (2 * MathF.Sqrt(a));
            float term2 = MathF.Sqrt(c) / (2 * MathF.Sqrt(a));
            float integral = MathF.Asinh(s / (2 * MathF.Sqrt(a * c))) / MathF.Sqrt(a);

            return (term1 - term2) / s;
        }

        public List<Path2Point> SampleAdaptive(List<Path2Point> points, float tolerance = 0.01f)
        {
            if (points.Count == 0 || points[^1].Point != P1)
                points.Add(new Path2Point
                {
                    Point = P1,
                    Segment = this,
                    Time = 0
                });

            if (!IsLinear())
                SampleAdaptiveV2(0, 1, tolerance, points);

            if (points.Count == 0 || points[^1].Point != P2)
                points.Add(new Path2Point
                {
                    Point = P2,
                    Time = 1,
                    Segment = this  
                });

            return points;
        }

        public Bounds2 Bounds()
        {
            if (_bounds == null)
            {
                List<Vector2> points = [];

                if (IsLinear())
                {
                    points.Add(P1);
                    points.Add(P2);
                }
                else
                {
                    points.Add(Sample(0));
                    points.Add(Sample(1));

                    foreach (float t in FindExtremaX())
                    {
                        if (t >= 0 && t <= 1)
                            points.Add(Sample(t));
                    }

                    foreach (float t in FindExtremaY())
                    {
                        if (t >= 0 && t <= 1)
                            points.Add(Sample(t));
                    }
                }

                var result = new Bounds2
                {
                    Min = points[0],
                    Max = points[0]
                };

                foreach (var point in points)
                {
                    result.Min = Vector2.Min(result.Min, point);
                    result.Max = Vector2.Max(result.Max, point);
                }
                _bounds = result;
            }

            return _bounds.Value;
        }

        public List<float> FindExtremaX()
        {
            return FindExtrema(P1.X, C1.X, C2.X, P2.X);
        }

        public List<float> FindExtremaY()
        {
            return FindExtrema(P1.Y, C1.Y, C2.Y, P2.Y);
        }

        private List<float> FindExtrema(float p1, float c1, float c2, float p2)
        {
            float a = -3 * p1 + 9 * c1 - 9 * c2 + 3 * p2;
            float b = 6 * p1 - 12 * c1 + 6 * c2;
            float c = -3 * p1 + 3 * c1;

            // Solve the quadratic equation: at^2 + bt + c = 0
            var roots = new List<float>();
            if (Math.Abs(a) > 1e-6f)
            {
                float discriminant = b * b - 4 * a * c;
                if (discriminant >= 0)
                {
                    float sqrtD = MathF.Sqrt(discriminant);
                    roots.Add((-b + sqrtD) / (2 * a));
                    roots.Add((-b - sqrtD) / (2 * a));
                }
            }
            else if (Math.Abs(b) > 1e-6f)
            {
                roots.Add(-c / b);
            }

            return roots;
        }


        public List<Path2Point> SampleAdaptive(float tolerance = 0.01f)
        {
            return SampleAdaptive(new List<Path2Point>(), tolerance);
        }

        private void SampleAdaptive(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float tolerance, List<Vector2> points)
        {
            // Compute the chord length
            var chordLength = Vector2.Distance(p0, p3);

            // Compute the control net length
            var controlNetLength = Vector2.Distance(p0, p1) + Vector2.Distance(p1, p2) + Vector2.Distance(p2, p3);

            // Check if the curve segment is flat enough
            if (Math.Abs(controlNetLength - chordLength) <= tolerance)
            {
                // Flat enough: Add the end point of this segment
                points.Add(p3);
            }
            else
            {
                // Subdivide the curve
                var mid1 = (p0 + p1) * 0.5f;
                var mid2 = (p1 + p2) * 0.5f;
                var mid3 = (p2 + p3) * 0.5f;

                var mid4 = (mid1 + mid2) * 0.5f;
                var mid5 = (mid2 + mid3) * 0.5f;

                var mid6 = (mid4 + mid5) * 0.5f;

                // Recursively sample the left and right halves
                SampleAdaptive(p0, mid1, mid4, mid6, tolerance, points);
                SampleAdaptive(mid6, mid5, mid3, p3, tolerance, points);
            }
        }

        private void SampleAdaptiveV2(float t0, float t1, float tolerance, List<Path2Point> points)
        {
            var mid = (t0 + t1) / 2;
            var p0 = Sample(t0);
            var p1 = Sample(mid);
            var p2 = Sample(t1);

            var midp = (p0 + p2) / 2;

            var diff = (midp - p1).Length();
            if (diff > tolerance)
            {
                SampleAdaptiveV2(t0, mid, tolerance, points);
                SampleAdaptiveV2(mid, t1, tolerance, points);
            }
            else
            {
                if (points.Count == 0 || points[^1].Point != p2)
                    points.Add(new Path2Point
                    {
                        Point = p2,
                        Segment = this,
                        Time = t1
                    });
            }
        }

        public bool IsQuadratic(float tolerance = 1e-6f)
        {
            // Expected positions for control points to behave as quadratic
            var expectedC1 = (2f / 3f) * P1 + (1f / 3f) * P2;
            var expectedC2 = (1f / 3f) * P1 + (2f / 3f) * P2;

            // Check if the actual control points are close to the expected ones
            return Vector2.Distance(C1, expectedC1) < tolerance &&
                   Vector2.Distance(C2, expectedC2) < tolerance;
        }

        public bool IsLinear(float tolerance = 1e-6f)
        {
            var v1 = C1 - P1;
            var v2 = C2 - P1;
            var v3 = P2 - P1;

            var cross1 = Math.Abs(v1.X * v3.Y - v1.Y * v3.X);
            var cross2 = Math.Abs(v2.X * v3.Y - v2.Y * v3.X);

            return cross1 < tolerance && cross2 < tolerance;
        }

        public Vector2 Sample(float t)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            var point = uuu * P1; // (1-t)^3 * P1
            point += 3 * uu * t * C1; // 3(1-t)^2 * t * C1
            point += 3 * u * tt * C2; // 3(1-t) * t^2 * C2
            point += ttt * P2;        // t^3 * P2

            return point;
        }

        public Vector2 Tangent(float t)
        {
            float u = 1 - t;

            // First derivative of the cubic Bézier curve
            var tangent =
                3 * u * u * (C1 - P1) +
                6 * u * t * (C2 - C1) +
                3 * t * t * (P2 - C2);

            return tangent;
        }

        public float Length(float tolerance = 0.01f)
        {
            if (_length == null)
            {
                if (IsLinear())
                    _length = (P2 - P1).Length();

                else if (IsQuadratic())
                    _length = QuadraticLength();

                else
                {
                    _length = 0;
                    var samples = SampleAdaptive(tolerance);
                    for (var i = 0; i < samples.Count - 1; i++)
                        _length += (samples[i].Point - samples[i + 1].Point).Length();
                }
            }

            return _length.Value;
        }


        public static float GoldenSectionSearch(Func<float, float> f, float a, float b, float tol = 1e-5f)
        {
            float gr = 1.618033988749895f; // Golden ratio

            float c = b - (b - a) / gr;
            float d = a + (b - a) / gr;

            while (Math.Abs(c - d) > tol)
            {
                if (f(c) < f(d))
                    b = d;
                else
                    a = c;

                c = b - (b - a) / gr;
                d = a + (b - a) / gr;
            }

            return (b + a) / 2;
        }

        public float FindClosestT(Vector2 point, float tol = 1e-5f)
        {
            return GoldenSectionSearch(t => Vector2.DistanceSquared(Sample(t), point), 0f, 1f, tol);
        }

        public void Invalidate()
        {
            _version++;
            _bounds = null;
            _length = null;
        }

        public void Transform(Matrix3x2 matrix)
        {
            P1 = Vector2.Transform(P1, matrix);
            P2 = Vector2.Transform(P2, matrix);
            C1 = Vector2.Transform(C1, matrix);
            C2 = Vector2.Transform(C2, matrix);
            Invalidate();
        }

        public IEnumerable<Vector2> Points
        {
            get
            {
                yield return P1;
                yield return C1;
                yield return C2;
                yield return P2;
            }
        }
  

        public Vector2 P1;
        public Vector2 P2;
        public Vector2 C1;
        public Vector2 C2;
    }

    public class Path2
    {
        protected Vector2 _currentPoint;
        protected LinkedList<Path2Segment> _segments = [];
        long _version;

        public void Clear()
        {
            _segments.Clear();
            _currentPoint = Vector2.Zero;
            _version++;
        }

        public void SplitAtPoint(Vector2 point, float tolerance = 1e-5f)
        {
            SplitAtPoint(FindClosestPoint(point, tolerance));
        }

        public void SplitAtPoint(Path2Point point)
        {
            var L1 = Vector2.Lerp(point.Segment.P1, point.Segment.C1, point.Time);
            var L2 = Vector2.Lerp(point.Segment.C1, point.Segment.C2, point.Time);
            var L3 = Vector2.Lerp(point.Segment.C2, point.Segment.P2, point.Time);

            // Level 2 interpolation
            var M1 = Vector2.Lerp(L1, L2, point.Time);
            var M2 = Vector2.Lerp(L2, L3, point.Time);

            // Level 3 interpolation
            var Q = Vector2.Lerp(M1, M2, point.Time);

            point.Segment.C1 = L1;
            point.Segment.C2 = M1;
            point.Segment.P2 = Q;

            var newSegment = new Path2Segment()
            {
                P1 = Q,
                C1 = M2,
                C2 = L3,
                P2 = point.Segment.P2
            };

            var node = _segments.Find(point.Segment);
            _segments.AddAfter(node!, newSegment);
        }

        public Path2Point FindClosestPoint(Vector2 p, float tolerance = 1e-5f)
        {
            var result = new Path2Point();
            var minDistance = float.PositiveInfinity;

            foreach (var segment in _segments)
            {
                var t = segment.FindClosestT(p, tolerance);
                var pt = segment.Sample(t);
                var distance = Vector2.DistanceSquared(pt, p);
                if (distance < minDistance)
                {
                    result.Segment = segment;
                    result.Time = t;
                    result.Point = pt;
                    minDistance = distance;
                }
            }

            return result;
        }

        public Bounds2 Bounds()
        {
            if (_segments.Count == 0)
                return Bounds2.Zero;

            var result = _segments.First!.Value.Bounds();

            foreach (var segment in _segments.Skip(1))
            {
                var segBounds = segment.Bounds();
                result.Max = Vector2.Max(result.Max, segBounds.Max);
                result.Min = Vector2.Min(result.Min, segBounds.Min);
            }

            return result;
        }

        public Path2Segment AddSegment(Path2Segment segment)
        {
            _segments.AddLast(segment);
            _currentPoint = segment.P2;
            _version++;
            return segment;
        }

        public List<Vector2> SamplesFixed(float dt)
        {
            var result = new List<Vector2>();
            foreach (var segment in _segments)
            {
                for (float t = 0; t <= 1; t += dt)
                {
                    if (t != 1 && t + dt > 1)
                        t = 1;
                    var point = segment.Sample(t);
                    if (result.Count == 0 || point != result[result.Count - 1])
                        result.Add(point);
                }
            }
            return result;
        }

        public List<Path2Point> SamplesAdaptive(float tolerance = 0.01f)
        {
            var result = new List<Path2Point>();
            foreach (var segment in _segments)
                segment.SampleAdaptive(result, tolerance);
            return result;
        }

        public Vector2 SampleAtLen(float len)
        {
            if (_segments.Count == 0)
                return Vector2.Zero;

            var totLen = Length();

            if (len > totLen)
            {
                var lastSeg = _segments.Last!.Value;
                var tan = Vector2.Normalize(lastSeg.Tangent(1));
                return lastSeg.P2 + tan * (len - totLen);
            }

            if (len < 0)
            {
                var firstSeg = _segments.First!.Value;
                var tan = Vector2.Normalize(firstSeg.Tangent(0));
                return firstSeg.P1 + tan * len;
            }

            var curLen = 0f;
            foreach (var segment in _segments)
            {
                var segLen = segment.Length();

                if (len <= curLen + segLen)
                {
                    var t = (len - curLen) / segLen;
                    return segment.Sample(t);
                }
                curLen += segLen;
            }

            return Vector2.Zero;
        }

        public float Length()
        {
            var result = 0f;

            foreach (var segment in _segments)
                result += segment.Length();

            return result;
        }

        public DiscreteFunction ToFunctionY(float dt, float sign = 1)
        {
            var samples = SamplesFixed(dt);

            float lastY = float.NegativeInfinity;

            var newPoints = new List<Vector2>();

            foreach (var point in samples)
            {
                var signY = sign * point.Y;

                if (signY >= lastY)
                {
                    newPoints.Add(new Vector2(point.Y, point.X));
                    lastY = signY;
                }
            }

            if (sign == -1)
                newPoints.Reverse();

            return new DiscreteFunction() { Points = newPoints.ToArray() };
        }

        public DiscreteFunction ToFunctionX(float dt)
        {
            var samples = SamplesFixed(dt);
            float lastX = float.NegativeInfinity;

            var newPoints = new List<Vector2>();

            foreach (var point in samples)
            {
                if (point.X >= lastX)
                {
                    newPoints.Add(point);
                    lastX = point.X;
                }
            }

            return new DiscreteFunction() { Points = newPoints.ToArray() };
        }

        public void MoveTo(Vector2 p1)
        {
            _currentPoint = p1;
        }

        public void LineTo(Vector2 p2)
        {
            AddSegment(new Path2Segment
            {
                P1 = _currentPoint,
                P2 = p2,
                C1 = _currentPoint,
                C2 = p2
            });
        }

        public void BezierTo(Vector2 p2, Vector2 c1, Vector2 c2)
        {
            AddSegment(new Path2Segment
            {
                P1 = _currentPoint,
                P2 = p2,
                C1 = c1,
                C2 = c2
            });
        }

        public void QuadraticTo(Vector2 p2, Vector2 c)
        {
            var c1 = (2f / 3f) * c + (1f / 3f) * _currentPoint;
            var c2 = (2f / 3f) * c + (1f / 3f) * p2;

            AddSegment(new Path2Segment
            {
                P1 = _currentPoint,
                P2 = p2,
                C1 = c1,
                C2 = c2
            });
        }

        public void ParseSvgPath(string svgPath)
        {
            int i = 0;
            var startPoint = Vector2.Zero;

            List<float> args = [];

            while (i < svgPath.Length)
            {
                char command = svgPath[i++];
                bool isRelative = char.IsLower(command);

                // Extract arguments for the command
                args.Clear();

                while (i < svgPath.Length && (char.IsDigit(svgPath[i]) || svgPath[i] == '-' || svgPath[i] == '.' || svgPath[i] == ',' || svgPath[i] == ' '))
                {
                    if (svgPath[i] == ',' || svgPath[i] == ' ')
                        i++; // Skip commas

                    int start = i;

                    // Parse the number
                    while (i < svgPath.Length && (char.IsDigit(svgPath[i]) || svgPath[i] == '-' || svgPath[i] == '.'))
                        i++;

                    if (start < i)
                        args.Add(float.Parse(svgPath.AsSpan(start, i - start), CultureInfo.InvariantCulture));
                }

                // Process commands
                switch (char.ToUpper(command))
                {
                    case 'M': // Move to
                        for (int j = 0; j < args.Count; j += 2)
                        {
                            var point = new Vector2(args[j], args[j + 1]);
                            if (isRelative)
                                point += _currentPoint;

                            MoveTo(point);
                            startPoint = _currentPoint; // Update start point
                        }
                        break;

                    case 'L': // Line to
                        for (int j = 0; j < args.Count; j += 2)
                        {
                            var point = new Vector2(args[j], args[j + 1]);
                            if (isRelative) point += _currentPoint;

                            LineTo(point);
                        }
                        break;

                    case 'Q': // Quadratic Bézier
                        for (int j = 0; j < args.Count; j += 4)
                        {
                            var c = new Vector2(args[j], args[j + 1]);
                            var p2 = new Vector2(args[j + 2], args[j + 3]);

                            if (isRelative)
                            {
                                c += _currentPoint;
                                p2 += _currentPoint;
                            }

                            QuadraticTo(p2, c);
                        }
                        break;


                    case 'C': // Cubic Bézier
                        for (int j = 0; j < args.Count; j += 6)
                        {
                            var c1 = new Vector2(args[j], args[j + 1]);
                            var c2 = new Vector2(args[j + 2], args[j + 3]);
                            var p2 = new Vector2(args[j + 4], args[j + 5]);

                            if (isRelative)
                            {
                                c1 += _currentPoint;
                                c2 += _currentPoint;
                                p2 += _currentPoint;
                            }

                            BezierTo(p2, c1, c2);
                        }
                        break;

                    case 'Z': // Close path
                        LineTo(startPoint);
                        break;

                    default:
                        throw new InvalidOperationException($"Unsupported SVG command: {command}");
                }
            }
        }

        public void Transform(Matrix3x2 matrix)
        {
            foreach (var segment in _segments)
                segment.Transform(matrix);

        }

        public Path2Segment this[int index]
        {
            get => _segments.ElementAt(index);
        } 

        public LinkedList<Path2Segment> Segments => _segments;

        public long Version => _version;
    }
}
