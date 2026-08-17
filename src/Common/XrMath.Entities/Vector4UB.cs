using System.Diagnostics.CodeAnalysis;

namespace XrMath
{
    public struct Vector4UB
    {
        public Vector4UB() { }

        public Vector4UB(byte x, byte y, byte z, byte w)
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
        public static bool operator ==(Vector4UB left, Vector4UB right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Vector4UB left, Vector4UB right)
        {
            return !(left == right);
        }

        public readonly Vector4I ToVector4I()
        {
            return new Vector4I(X, Y, Z, W);
        }

        public byte X;

        public byte Y;

        public byte Z;

        public byte W;
    }
}
