using System.Numerics;

namespace XrMath
{
    public struct Pose3
    {
        public Vector3 Position;

        public Quaternion Orientation;

        public readonly override bool Equals(object obj)
        {
            if (obj is Pose3 other)
                return other.Position == Position && other.Orientation == Orientation;
            return false;
        }

        public readonly override int GetHashCode()
        {
            return Position.GetHashCode() ^ Orientation.GetHashCode();
        }

        public static bool operator ==(Pose3 a, Pose3 b) => a.Equals(b);

        public static bool operator !=(Pose3 a, Pose3 b) => !a.Equals(b);


        public static readonly Pose3 Identity = new()
        {
            Orientation = Quaternion.Identity
        };
    }
}
