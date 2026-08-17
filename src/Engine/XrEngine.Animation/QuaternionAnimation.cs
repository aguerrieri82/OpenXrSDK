using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace XrEngine.Animation
{
    public class QuaternionAnimation : BaseAnimation<Quaternion>
    {
        protected override Quaternion Interpolate(Quaternion startValue, Quaternion endValue, float t)
        {
            return Quaternion.Slerp(startValue, endValue, t);
        }
    }
}
