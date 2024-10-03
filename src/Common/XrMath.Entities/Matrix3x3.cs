namespace XrMath
{
    public struct Matrix3x3
    {
        public Matrix3x3()
        {

        }
        public Matrix3x3(params float[] values)
        {
            M11 = values[0];
            M12 = values[1];
            M13 = values[2];

            M21 = values[3];
            M22 = values[4];
            M23 = values[5];

            M31 = values[6];
            M32 = values[7];
            M33 = values[8];

        }

        public bool IsIdentity
        {
            get
            {
                return M11 == 1 && M12 == 0 && M13 == 0 &&
                       M21 == 0 && M22 == 1 && M23 == 0 &&
                       M31 == 0 && M32 == 0 && M33 == 1;
            }
        }

        public override bool Equals(object? obj)
        {
            if (!(obj is Matrix3x3))
                return false;

            return Equals((Matrix3x3)obj);
        }

        public bool Equals(Matrix3x3 other)
        {
            return M11 == other.M11 && M12 == other.M12 && M13 == other.M13 &&
                   M21 == other.M21 && M22 == other.M22 && M23 == other.M23 &&
                   M31 == other.M31 && M32 == other.M32 && M33 == other.M33;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 31 + M11.GetHashCode();
            hash = hash * 31 + M12.GetHashCode();
            hash = hash * 31 + M13.GetHashCode();
            hash = hash * 31 + M21.GetHashCode();
            hash = hash * 31 + M22.GetHashCode();
            hash = hash * 31 + M23.GetHashCode();
            hash = hash * 31 + M31.GetHashCode();
            hash = hash * 31 + M32.GetHashCode();
            hash = hash * 31 + M33.GetHashCode();
            return hash;
        }

        public static bool operator ==(Matrix3x3 left, Matrix3x3 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Matrix3x3 left, Matrix3x3 right)
        {
            return !(left == right);
        }

        public static Matrix3x3 CreateScale(float scaleX, float scaleY)
        {
            return new Matrix3x3
            {
                M11 = scaleX,
                M12 = 0,
                M13 = 0,
                M21 = 0,
                M22 = scaleY,
                M23 = 0,
                M31 = 0,
                M32 = 0,
                M33 = 1
            };
        }

        public static Matrix3x3 CreateRotationX(float radians)
        {
            float cos = (float)Math.Cos(radians);
            float sin = (float)Math.Sin(radians);

            return new Matrix3x3
            {
                M11 = 1,
                M12 = 0,
                M13 = 0,
                M21 = 0,
                M22 = cos,
                M23 = -sin,
                M31 = 0,
                M32 = sin,
                M33 = cos
            };
        }

        public static Matrix3x3 CreateRotationY(float radians)
        {
            float cos = (float)Math.Cos(radians);
            float sin = (float)Math.Sin(radians);

            return new Matrix3x3
            {
                M11 = cos,
                M12 = 0,
                M13 = sin,
                M21 = 0,
                M22 = 1,
                M23 = 0,
                M31 = -sin,
                M32 = 0,
                M33 = cos
            };
        }

        public static Matrix3x3 CreateRotationZ(float radians)
        {
            float cos = (float)Math.Cos(radians);
            float sin = (float)Math.Sin(radians);

            return new Matrix3x3
            {
                M11 = cos,
                M12 = -sin,
                M13 = 0,
                M21 = sin,
                M22 = cos,
                M23 = 0,
                M31 = 0,
                M32 = 0,
                M33 = 1
            };
        }

        public static Matrix3x3 CreateTranslation(float translateX, float translateY)
        {
            return new Matrix3x3
            {
                M11 = 1,
                M12 = 0,
                M13 = translateX,
                M21 = 0,
                M22 = 1,
                M23 = translateY,
                M31 = 0,
                M32 = 0,
                M33 = 1
            };
        }

        public static Matrix3x3 operator *(Matrix3x3 a, Matrix3x3 b)
        {
            return new Matrix3x3
            {
                M11 = a.M11 * b.M11 + a.M12 * b.M21 + a.M13 * b.M31,
                M12 = a.M11 * b.M12 + a.M12 * b.M22 + a.M13 * b.M32,
                M13 = a.M11 * b.M13 + a.M12 * b.M23 + a.M13 * b.M33,

                M21 = a.M21 * b.M11 + a.M22 * b.M21 + a.M23 * b.M31,
                M22 = a.M21 * b.M12 + a.M22 * b.M22 + a.M23 * b.M32,
                M23 = a.M21 * b.M13 + a.M22 * b.M23 + a.M23 * b.M33,

                M31 = a.M31 * b.M11 + a.M32 * b.M21 + a.M33 * b.M31,
                M32 = a.M31 * b.M12 + a.M32 * b.M22 + a.M33 * b.M32,
                M33 = a.M31 * b.M13 + a.M32 * b.M23 + a.M33 * b.M33,
            };
        }

        public static (float x, float y) Transform(Matrix3x3 matrix, float x, float y)
        {
            return (
                x * matrix.M11 + y * matrix.M12 + matrix.M13,
                x * matrix.M21 + y * matrix.M22 + matrix.M23
            );

        }

        public static Matrix3x3 Identity => new()
        {
            M11 = 1,
            M22 = 1,
            M33 = 1
        };



        public float M11, M12, M13;
        public float M21, M22, M23;
        public float M31, M32, M33;
    }
}
