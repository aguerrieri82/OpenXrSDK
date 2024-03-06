using System.Numerics;

namespace Xr.Engine
{
    public struct Capsule3
    {
        public float Height;

        public float Radius;

        public Vector3 Position;

        public Quaternion Orientation;


        public Sphere TopSphere => new Sphere
        {
            Center = Position + Vector3.Transform(new Vector3(0, Height / 2, 0), Orientation),
            Radius = Radius,    
        };

        public Sphere BottomSphere => new Sphere
        {
            Center = Position + Vector3.Transform(new Vector3(0, -Height / 2, 0), Orientation),
            Radius = Radius,
        };
    }
}
