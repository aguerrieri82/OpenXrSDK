using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace XrEngine.Animation
{
    public class QuaternionHandler : IAnimationValueHandler<Quaternion>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Quaternion Interpolate(Quaternion start, Quaternion end, float t)
        {
            return Quaternion.Slerp(start, end, t);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Quaternion Combine(Quaternion value, Quaternion offset)
        {
            return value * offset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Quaternion Remove(Quaternion value, Quaternion offset)
        {
            return value * Quaternion.Inverse(offset);
        }
    }
}
