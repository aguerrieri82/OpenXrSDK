using System.Numerics;

namespace XrMath
{
    public struct Pose3
    {
        public Pose3()
        {
            Orientation = Quaternion.Identity;
        }

        public Pose3(Vector3 position)
        {
            Position = position;
            Orientation = Quaternion.Identity;
        }

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

        public Vector3 Position;

        public Quaternion Orientation;


        public static bool operator ==(Pose3 a, Pose3 b) => a.Equals(b);

        public static bool operator !=(Pose3 a, Pose3 b) => !a.Equals(b);


        public static readonly Pose3 Identity = new()
        {
            Orientation = Quaternion.Identity
        };
    }
}
