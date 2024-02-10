using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.WebLink.Entities
{
    public static class MathExtensions
    {
        public static bool Similar(this Quaternion value, Quaternion other, float epsilon)
        {
            return MathF.Abs(value.X - other.X) < epsilon &&
                MathF.Abs(value.Y - other.Y) < epsilon &&
                MathF.Abs(value.Z - other.Z) < epsilon &&
                MathF.Abs(value.W - other.W) < epsilon;
        }

        public static bool Similar(this Vector3 value, Vector3 other, float epsilon)
        {
            return MathF.Abs(value.X - other.X) < epsilon &&
                MathF.Abs(value.Y - other.Y) < epsilon &&
                MathF.Abs(value.Z - other.Z) < epsilon;
        }
    }
}
