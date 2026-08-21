namespace XrEngine
{
    internal readonly struct MeshEdgeKey : IEquatable<MeshEdgeKey>
    {
        public MeshEdgeKey(uint a, uint b)
        {
            if (a <= b)
            {
                A = a;
                B = b;
            }
            else
            {
                A = b;
                B = a;
            }
        }

        public MeshEdgeKey(int a, int b)
            : this((uint)a, (uint)b)
        {
        }

        public readonly ulong Packed => ((ulong)A << 32) | B;

        public bool Equals(MeshEdgeKey other)
        {
            return A == other.A && B == other.B;
        }

        public override bool Equals(object? obj)
        {
            return obj is MeshEdgeKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)A * 397) ^ (int)B;
            }
        }

        public readonly uint A;

        public readonly uint B;
    }
}