using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine.Animation
{
    public class FloatAnimation : BaseAnimation<float>
    {

        protected override float Interpolate(float startValue, float endValue, float t)
        {
            return startValue + (endValue - startValue) * t;
        }
    }
}
