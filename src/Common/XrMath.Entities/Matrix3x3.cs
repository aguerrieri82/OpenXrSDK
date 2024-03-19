namespace XrMath
{
    public struct Matrix3x3
    {
        public static Matrix3x3 Rotation(float angleRad)
        {
            float cosTheta = MathF.Cos(angleRad);
            float sinTheta = MathF.Sin(angleRad);

            var res = new Matrix3x3
            {
                M00 = cosTheta,
                M01 = -sinTheta,

                M10 = sinTheta,
                M11 = cosTheta,

                M22 = 1
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
