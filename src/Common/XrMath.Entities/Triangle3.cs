using System.Numerics;

namespace XrMath
{
    public struct Triangle3
    {
        public Triangle3()
        {

        }

        public Triangle3(Vector3 v0, Vector3 v1, Vector3 v2)
        {
            V0 = v0;
            V1 = v1;
            V2 = v2;
        }

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

        public IEnumerable<uint> Indices
        {
            get
            {
                yield return I0;
                yield return I1;
                yield return I2;
            }
        }

        public readonly Vector3 Min => Vector3.Min(Vector3.Min(V0, V1), V2);

        public readonly Vector3 Max => Vector3.Max(Vector3.Max(V0, V1), V2);

        public readonly Vector3 Center => (V0 + V1 + V2) / 3.0f;

        public readonly Vector3 Cross => Vector3.Cross(V1 - V0, V2 - V0);

        public readonly float AreaSq => Cross.LengthSquared();

        public int Id;

        public Vector3 V0;

        public Vector3 V1;

        public Vector3 V2;

        public uint I0;

        public uint I1;

        public uint I2;

    }
}
