using System;
using System.Collections.Generic;
using System.Text;
using XrMath;

namespace XrEngine.OpenXr
{
    public interface IEnvRayCollider
    {
        bool CastRay(Ray3 ray, float maxDistance, out Pose3 result);

        bool IsEnabled { get; set; } 
    }
}
