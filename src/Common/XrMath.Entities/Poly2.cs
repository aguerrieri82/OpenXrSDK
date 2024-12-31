using System.Numerics;


namespace XrMath
{
    public struct Poly2
    {
        public Poly2()
        {
            Points = [];
            IsClosed = true;
        }

        public Poly2(Vector2[] points, bool isClosed = true)
        {
            Points = points;
            IsClosed = isClosed;
        }


        public static implicit operator Vector2[](Poly2 poly)
        {
            return poly.Points;
        }

        public Vector2[] Points;

        public bool IsClosed;
    }
}
