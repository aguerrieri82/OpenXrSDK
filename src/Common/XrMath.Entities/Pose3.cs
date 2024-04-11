using System.Numerics;

namespace XrMath
{
    public struct Pose3
    {
        public Vector3 Position;

        public Quaternion Orientation;


        public static readonly Pose3 Identity = new()
        {
            Orientation = Quaternion.Identity
        };
    }
}
