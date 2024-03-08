using System.Numerics;

namespace Xr.Engine
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

        public unsafe static Matrix4x4 InvertRigidBody(this Matrix4x4 src)
        {
            var result = stackalloc float[16];
            var srcArray = new Span<float>((float*)&src, 16);

            result[0] = srcArray[0];
            result[1] = srcArray[4];
            result[2] = srcArray[8];
            result[3] = 0.0f;
            result[4] = srcArray[1];
            result[5] = srcArray[5];
            result[6] = srcArray[9];
            result[7] = 0.0f;
            result[8] = srcArray[2];
            result[9] = srcArray[6];
            result[10] = srcArray[10];
            result[11] = 0.0f;
            result[12] = -(srcArray[0] * srcArray[12] + srcArray[1] * srcArray[13] + srcArray[2] * srcArray[14]);
            result[13] = -(srcArray[4] * srcArray[12] + srcArray[5] * srcArray[13] + srcArray[6] * srcArray[14]);
            result[14] = -(srcArray[8] * srcArray[12] + srcArray[9] * srcArray[13] + srcArray[10] * srcArray[14]);
            result[15] = 1.0f;

            return *(Matrix4x4*)result;
        }

        public static Quaternion QuatFromForwardUp(Vector3 forward, Vector3 up)
        {
            var lookAt = Matrix4x4.CreateLookAt(Vector3.Zero, forward, up);
            Matrix4x4.Invert(lookAt, out var rotMatrix);
            return Quaternion.CreateFromRotationMatrix(rotMatrix);
        }

        public static Quaternion QuatFromForwardUp2(Vector3 forward, Vector3 up)
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
                float num = MathF.Sqrt(num8 + 1f);
                quaternion.W = num * 0.5f;
                num = 0.5f / num;
                quaternion.X = (m12 - m21) * num;
                quaternion.Y = (m20 - m02) * num;
                quaternion.Z = (m01 - m10) * num;
                return quaternion;
            }
            if ((m00 >= m11) && (m00 >= m22))
            {
                float num7 = MathF.Sqrt(((1f + m00) - m11) - m22);
                float num4 = 0.5f / num7;
                quaternion.X = 0.5f * num7;
                quaternion.Y = (m01 + m10) * num4;
                quaternion.Z = (m02 + m20) * num4;
                quaternion.W = (m12 - m21) * num4;
                return quaternion;
            }
            if (m11 > m22)
            {
                float num6 = MathF.Sqrt(((1f + m11) - m00) - m22);
                float num3 = 0.5f / num6;
                quaternion.X = (m10 + m01) * num3;
                quaternion.Y = 0.5f * num6;
                quaternion.Z = (m21 + m12) * num3;
                quaternion.W = (m20 - m02) * num3;
                return quaternion;
            }
            float num5 = MathF.Sqrt(((1f + m22) - m00) - m11);
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
