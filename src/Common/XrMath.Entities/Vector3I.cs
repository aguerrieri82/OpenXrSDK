namespace XrMath
{
    public struct Vector3I : IEquatable<Vector3I>
    {
        public Vector3I()
        {
        }

        public Vector3I(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public readonly bool Equals(Vector3I other)
        {
            return X == other.X && Y == other.Y && Z == other.Z;
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is Vector3I other && Equals(other);
        }

        public readonly override int GetHashCode()
        {
            unchecked
            {
                var hash = X;
                hash = (hash * 397) ^ Y;
                hash = (hash * 397) ^ Z;
                return hash;
            }
        }

        public static bool operator ==(Vector3I left, Vector3I right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Vector3I left, Vector3I right)
        {
            return !left.Equals(right);
        }

        public int X;

        public int Y;

        public int Z;
    }
}