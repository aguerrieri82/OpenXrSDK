namespace XrEngine
{
    internal readonly struct MeshTriangleKey : IEquatable<MeshTriangleKey>
    {
        public MeshTriangleKey(uint a, uint b, uint c)
        {
            if (a > b)
                (a, b) = (b, a);

            if (b > c)
                (b, c) = (c, b);

            if (a > b)
                (a, b) = (b, a);

            A = a;
            B = b;
            C = c;
        }

        public MeshTriangleKey(int a, int b, int c)
            : this((uint)a, (uint)b, (uint)c)
        {
        }

        public bool Equals(MeshTriangleKey other)
        {
            return A == other.A && B == other.B && C == other.C;
        }

        public override bool Equals(object? obj)
        {
            return obj is MeshTriangleKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)A;
                hash = (hash * 397) ^ (int)B;
                hash = (hash * 397) ^ (int)C;
                return hash;
            }
        }

        public readonly uint A;

        public readonly uint B;

        public readonly uint C;
    }
}