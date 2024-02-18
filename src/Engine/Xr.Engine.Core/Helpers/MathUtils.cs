using System.Numerics;
using System.Reflection;

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

        public static Quaternion QuatDiff(Quaternion to , Quaternion from)
        {
            return to * Quaternion.Inverse(from);
        }

        public static Quaternion QuatAdd(Quaternion start, Quaternion diff)
        {
            return diff * start;
        }

    }
}
