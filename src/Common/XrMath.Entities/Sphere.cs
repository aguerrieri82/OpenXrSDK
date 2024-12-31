using System.Numerics;

namespace XrMath
{
    public struct Sphere
    {
        public Sphere()
        {
        }

        public Sphere(Vector3 center, float radius)
        {
            Center = center;
            Radius = radius;
        }

        public Vector3 Center;

        public float Radius;
    }
}
