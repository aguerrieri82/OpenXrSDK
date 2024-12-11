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

        public float Length => _path.Length();

        public bool IsClosed => false;

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
            var points = _path.SamplesAdaptive(tolerance);

            return points.Select(a => new CurvePoint
            {
                Position = a.Point,
                Length = 0,
                Tangent = a.Segment.Tangent(a.Time)
            });
        }
    }
}
