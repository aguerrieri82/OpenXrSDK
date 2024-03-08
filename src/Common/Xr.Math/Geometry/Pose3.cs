using System.Numerics;

namespace Xr.Math
{
    public struct Pose3
    {
        public Vector3 Position;

        public Quaternion Orientation;

        public static Pose3 operator *(Pose3 a, Pose3 b)
        {
            return a.Multiply(b);
        }
    }
}
