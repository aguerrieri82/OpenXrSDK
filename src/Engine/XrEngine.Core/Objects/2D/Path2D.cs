using System.Numerics;
using XrMath;

namespace XrEngine
{
    public class Path2D : ICurve2D
    {
        readonly Path2 _path;

        public Path2D(Path2 path)
        {
            _path = path;
        }


        public Vector2 GetPointAtTime(float t)
        {
            throw new NotImplementedException();
        }

        public Vector2 GetTangentAtTime(float t)
        {
            return _path.SampleAtLen(t * _path.Length());
        }

        public float GetTimeAtLength(float length)
        {
            return length / _path.Length();
        }

        public IEnumerable<CurvePoint> Sample(float tolerance, int maxPoints)
        {
            List<Path2Point> points = _path.SamplesAdaptive(tolerance);
            float pathLen = _path.Length();

            Path2Segment? lastSeg = null;
            float segLen = 0f;
            float totLen = 0f;
            foreach (Path2Point point in points)
            {
                if (lastSeg != point.Segment)
                {
                    totLen += segLen;
                    lastSeg = point.Segment;
                    segLen = lastSeg.Length();
                }

                CurvePoint cPoint = new CurvePoint
                {
                    Position = point.Point,
                    Length = totLen + (segLen * point.Time),
                    Tangent = point.Segment.Tangent(point.Time),
                };

                cPoint.Time = cPoint.Length / pathLen;

                yield return cPoint;
            }

        }

        public float Length => _path.Length();

        public bool IsClosed => false;

    }
}
