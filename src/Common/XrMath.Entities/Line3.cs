using System.Numerics;


namespace XrMath
{
    public struct Line3
    {
        public Line3()
        {
        }

        public Line3(Vector3 from, Vector3 to)
        {
            From = from;
            To = to;
        }   

        public Vector3 From;

        public Vector3 To;
    }
}
