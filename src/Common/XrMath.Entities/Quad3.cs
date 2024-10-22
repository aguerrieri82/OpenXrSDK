using System.Numerics;

namespace XrMath
{
    public struct Quad3
    {
        public readonly override int GetHashCode()
        {
            return Pose.GetHashCode() ^ Size.GetHashCode();
        }

        public readonly override bool Equals(object obj)
        {
            if (obj is Quad3 other)
                return other.Size == Size && other.Pose == Pose;
            return false;
        }

        public static bool operator ==(Quad3 a, Quad3 b) => a.Equals(b);

        public static bool operator !=(Quad3 a, Quad3 b) => !a.Equals(b);

        public Pose3 Pose;

        public Vector2 Size;
    }
}
