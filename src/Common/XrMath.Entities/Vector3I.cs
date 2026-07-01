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

        public static Vector3I operator -(Vector3I left, Vector3I right)
        {
            return new Vector3I
            {
                X = left.X - right.X,
                Y = left.Y - right.Y,
                Z = left.Z - right.Z
            };
        }

        public static Vector3I operator +(Vector3I left, Vector3I right)
        {
            return new Vector3I
            {
                X = left.X + right.X,
                Y = left.Y + right.Y,
                Z = left.Z + right.Z
            };
        }

        public override string ToString()
        {
            return $"<{X},{Y},{Z}>";
        }

        public static readonly Vector3I Zero = new();


        public int X;

        public int Y;

        public int Z;
    }
}