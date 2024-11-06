using System.Numerics;

namespace XrEngine
{
    public struct CurvePoint
    {
        public Vector2 Position;
        public Vector2 Tangent;
        public float Time;
        public float Length;
    }

    public interface ICurve2D
    {
        IEnumerable<CurvePoint> Sample(float tolerance, int maxPoints);

        Vector2 GetPointAtTime(float t);

        Vector2 GetTangentAtTime(float t);

        float GetTimeAtLength(float length);

        float Length { get; }

        bool IsClosed { get; }
    }
}
