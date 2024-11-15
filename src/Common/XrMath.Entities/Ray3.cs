using System.Numerics;


namespace XrMath
{
    public struct Ray3
    {
        public Ray3()
        {

        }

        public Ray3(Vector3 origin, Vector3 direction, float roll = 0)
        {
            Origin = origin;
            Direction = Vector3.Normalize(direction);
        }

        public Vector3 Origin;

        public Vector3 Direction;

        public float Roll;

    }
}
