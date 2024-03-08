using System.Numerics;

namespace Xr.Math
{
    public struct Capsule3
    {
        public float Height;

        public float Radius;

        public Pose3 Pose;

        public Sphere TopSphere => new()
        {
            Center = Pose.Position + Vector3.Transform(new Vector3(0, Height / 2, 0), Pose.Orientation),
            Radius = Radius,
        };

        public Sphere BottomSphere => new()
        {
            Center = Pose.Position + Vector3.Transform(new Vector3(0, -Height / 2, 0), Pose.Orientation),
            Radius = Radius,
        };
    }
}
