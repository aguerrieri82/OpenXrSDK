using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace OpenXr.WebLink.Entities
{
    public struct Pose
    {
        public Quaternion Orientation;

        public Vector3 Position;

        public override readonly string ToString()
        {
            return string.Format("O: {0} - P: {1}", Orientation, Position);
        }

        public override readonly bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is Pose other)
                return other.Position.Equals(Position) &&
                       other.Orientation.Equals(Orientation);
            return false;
        }

        public bool Similar(Pose other, float epsilon)
        {
            return other.Position.Similar(Position, epsilon) &&
                   other.Orientation.Similar(Orientation, epsilon);
        }


        public override readonly int GetHashCode()
        {
            return Position.GetHashCode() ^ Orientation.GetHashCode();
        }
    }
}
