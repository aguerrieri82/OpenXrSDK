using System.Runtime.CompilerServices;

namespace XrEngine.Animation
{
    public class FloatArrayHandler : IAnimationValueHandler<float[]>
    {

        public float[] Interpolate(float[] start, float[] end, float t)
        {
            var result = new float[start.Length];

            for (var i = 0; i < result.Length; i++)
                result[i] = start[i] + (end[i] - start[i]) * t;

            return result;
        }

        public float[] Combine(float[] value, float[] offset)
        {
            if (offset.Length == 0)
                return value;

            var result = new float[value.Length];

            for (var i = 0; i < result.Length; i++)
                result[i] = value[i] + offset[i];

            return result;
        }

        public float[] Remove(float[] value, float[] offset)
        {
            if (offset.Length == 0)
                return value;

            var result = new float[value.Length];

            for (var i = 0; i < result.Length; i++)
                result[i] = value[i] - offset[i];

            return result;
        }

        public float[] Identity => [];
    }
}