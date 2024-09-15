using System.Diagnostics.CodeAnalysis;

namespace XrMath
{
    public struct Vector2I
    {
        public Vector2I() { }

        public Vector2I(int x, int y)
        {
            X = x;
            Y = y;
        }

        public readonly override int GetHashCode()
        {
            return X ^ Y;
        }

        public readonly override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is Vector2I other)
                return other.X == X && other.Y == Y;
            return false;
        }
        public static bool operator ==(Vector2I left, Vector2I right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Vector2I left, Vector2I right)
        {
            return !(left == right);
        }


        public int X;

        public int Y;
    }
}
