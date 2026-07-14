using System.Numerics;

namespace XrMath
{
    public struct Bounds3
    {
        public Vector3 Max;

        public Vector3 Min;

        public readonly IEnumerable<Vector3> Points
        {
            get
            {
                yield return new Vector3(Min.X, Min.Y, Min.Z);
                yield return new Vector3(Min.X, Max.Y, Min.Z);
                yield return new Vector3(Max.X, Max.Y, Min.Z);
                yield return new Vector3(Max.X, Min.Y, Min.Z);
                yield return new Vector3(Min.X, Min.Y, Max.Z);
                yield return new Vector3(Min.X, Max.Y, Max.Z);
                yield return new Vector3(Max.X, Max.Y, Max.Z);
                yield return new Vector3(Max.X, Min.Y, Max.Z);
            }

        }

        public readonly bool Equals(Bounds3 other)
        {
            return Min == other.Min &&
                   Max == other.Max;
        }

        public override readonly bool Equals(object? obj)
        {
            return obj is Bounds3 other && Equals(other);
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(Min, Max);
        }

        public static bool operator ==(Bounds3 left, Bounds3 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Bounds3 left, Bounds3 right)
        {
            return !left.Equals(right);
        }

        public readonly Vector3 Size => Max - Min;

        public readonly Vector3 Center => (Max + Min) / 2;

        public static Bounds3 Zero { get; } = new();
    }
}
