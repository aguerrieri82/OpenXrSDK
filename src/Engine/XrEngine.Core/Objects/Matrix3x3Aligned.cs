using System.Numerics;
using XrMath;

namespace XrEngine
{
    public struct Matrix3x3Aligned
    {
        public Matrix3x3Aligned(Matrix3x3 source)
        {
            R1 = new Vector4(source.M11, source.M12, source.M13, 0);
            R2 = new Vector4(source.M21, source.M22, source.M23, 0);
            R3 = new Vector4(source.M31, source.M32, source.M33, 0);
        }

        public static implicit operator Matrix3x3Aligned(Matrix3x3 source) => new(source);

        public Vector4 R1;
        public Vector4 R2;
        public Vector4 R3;
    }
}