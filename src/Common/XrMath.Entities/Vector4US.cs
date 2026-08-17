using System.Diagnostics.CodeAnalysis;

namespace XrMath
{
    public struct Vector4US
    {
        public Vector4US() { }

        public Vector4US(ushort x, ushort y, ushort z, ushort w)
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
        public static bool operator ==(Vector4US left, Vector4US right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Vector4US left, Vector4US right)
        {
            return !(left == right);
        }

        public readonly Vector4I ToVector4I()
        {
            return new Vector4I(X, Y, Z, W);
        }

        public ushort X;

        public ushort Y;

        public ushort Z;

        public ushort W;
    }
}
