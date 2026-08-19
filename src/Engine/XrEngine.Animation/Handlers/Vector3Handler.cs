using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace XrEngine.Animation
{
    public class Vector3Handler : IAnimationValueHandler<Vector3>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 Interpolate(Vector3 start, Vector3 end, float t)
        {
            return Vector3.Lerp(start, end, t);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 Combine(Vector3 value, Vector3 offset)
        {
            return value + offset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 Remove(Vector3 value, Vector3 offset)
        {
            return value - offset;
        }
    }
}
