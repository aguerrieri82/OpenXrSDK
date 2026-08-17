using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace XrEngine.Animation
{
    public class Vector3Animation : BaseAnimation<Vector3>
    {

        protected override Vector3 Interpolate(Vector3 startValue, Vector3 endValue, float t)
        {
            return Vector3.Lerp(startValue, endValue, t);
        }
    }
}
