using System.Numerics;

namespace OpenXr.Engine
{
    public static class MathUtils
    {
        public static Vector3 ToVector3(float[] array)
        {
            return new Vector3(array[0], array[1], array[2]);
        }

        public static Color ToColor(float[] array)
        {
            return new Color(array[0], array[1], array[2], array[3]);
        }

        public static unsafe Matrix4x4 CreateMatrix(float[] values)
        {
            if (values.Length != 16)
                throw new ArgumentException();
            fixed (float* data = values)
                return *(Matrix4x4*)data;
        }

        public static Quaternion QuatFromForwardUp(Vector3 forward, Vector3 up)
        {
            Vector3 zAxis = Vector3.Normalize(forward);
            Vector3 xAxis = Vector3.Normalize(Vector3.Cross(up, zAxis));
            Vector3 yAxis = Vector3.Cross(zAxis, xAxis);

            float m00 = xAxis.X;
            float m01 = xAxis.Y;
            float m02 = xAxis.Z;
            float m10 = yAxis.X;
            float m11 = yAxis.Y;
            float m12 = yAxis.Z;
            float m20 = zAxis.X;
            float m21 = zAxis.Y;
            float m22 = zAxis.Z;

            float num8 = (m00 + m11) + m22;
            Quaternion quaternion = new Quaternion();
            if (num8 > 0f)
            {
                float num = (float)Math.Sqrt(num8 + 1f);
                quaternion.W = num * 0.5f;
                num = 0.5f / num;
                quaternion.X = (m12 - m21) * num;
                quaternion.Y = (m20 - m02) * num;
                quaternion.Z = (m01 - m10) * num;
                return quaternion;
            }
            if ((m00 >= m11) && (m00 >= m22))
            {
                float num7 = (float)Math.Sqrt(((1f + m00) - m11) - m22);
                float num4 = 0.5f / num7;
                quaternion.X = 0.5f * num7;
                quaternion.Y = (m01 + m10) * num4;
                quaternion.Z = (m02 + m20) * num4;
                quaternion.W = (m12 - m21) * num4;
                return quaternion;
            }
            if (m11 > m22)
            {
                float num6 = (float)Math.Sqrt(((1f + m11) - m00) - m22);
                float num3 = 0.5f / num6;
                quaternion.X = (m10 + m01) * num3;
                quaternion.Y = 0.5f * num6;
                quaternion.Z = (m21 + m12) * num3;
                quaternion.W = (m20 - m02) * num3;
                return quaternion;
            }
            float num5 = (float)Math.Sqrt(((1f + m22) - m00) - m11);
            float num2 = 0.5f / num5;
            quaternion.X = (m20 + m02) * num2;
            quaternion.Y = (m21 + m12) * num2;
            quaternion.Z = 0.5f * num5;
            quaternion.W = (m01 - m10) * num2;
            return quaternion;
        }
    

        public static Quaternion QuatDiff(Quaternion to, Quaternion from)
        {
            return to * Quaternion.Inverse(from);
        }

        public static Quaternion QuatAdd(Quaternion start, Quaternion diff)
        {
            return diff * start;
        }

    }
}
