using System.Diagnostics.CodeAnalysis;

namespace XrMath
{
    public struct Vector4I
    {
        public Vector4I() { }

        public Vector4I(int x, int y, int z, int w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public readonly override int GetHashCode()
        {
            return X ^ Y ^ Z ^ W;
        }

        public readonly override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is Vector4I other)
                return other.X == X && other.Y == Y && other.Z == Z && other.W == W;
            return false;
        }
        public static bool operator ==(Vector4I left, Vector4I right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Vector4I left, Vector4I right)
        {
            return !(left == right);
        }

        public int X;

        public int Y;

        public int Z;

        public int W;
    }
}
