using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace XrEngine.Animation
{
    public class FloatHandler : IAnimationValueHandler<float>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Interpolate(float start, float end, float t)
        {
            return start + (end - start) * t;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Combine(float value, float offset)
        {
            return value + offset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Remove(float value, float offset)
        {
            return value - offset;
        }
    }
}
