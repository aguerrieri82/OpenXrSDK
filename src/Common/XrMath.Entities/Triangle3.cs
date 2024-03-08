using System.Numerics;

namespace XrMath
{
    public struct Triangle3
    {
        public Vector3 V0;

        public Vector3 V1;

        public Vector3 V2;

        public Vector3 this[int index]
        {
            get => index switch
            {
                0 => V0,
                1 => V1,
                2 => V2,
                _ => throw new IndexOutOfRangeException()
            };
        }

        public IEnumerable<Vector3> Vertices
        {
            get
            {
                yield return V0;
                yield return V1;
                yield return V2;
            }
        }
    }
}
