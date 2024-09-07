namespace XrMath
{
    public struct Matrix3x3
    {
        public Matrix3x3()
        {

        }
        public Matrix3x3(params float[] values)
        {
            M00 = values[0];
            M01 = values[1];
            M02 = values[2];

            M10 = values[3];
            M11 = values[4];
            M12 = values[5];

            M20 = values[6];
            M21 = values[7];
            M22 = values[8];

        }

        public static Matrix3x3 Rotation(float angleRad)
        {
            float cosTheta = MathF.Cos(angleRad);
            float sinTheta = MathF.Sin(angleRad);

            var res = new Matrix3x3
            {
                M00 = cosTheta,
                M02 = sinTheta,
                M11 = 1,
                M20 = -sinTheta,
                M22 = cosTheta,
            };

            return res;
        }

        public static Matrix3x3 Identity => new()
        {
            M00 = 1,
            M11 = 1,
            M22 = 1
        };

        public float M00;
        public float M01;
        public float M02;

        public float M10;
        public float M11;
        public float M12;

        public float M20;
        public float M21;
        public float M22;
    }
}
