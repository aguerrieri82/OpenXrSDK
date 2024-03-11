using PhysX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrMath;

namespace XrEngine.Physics
{
    public static class PxExtensions
    {
        public static Pose3 ToPose3(this PxTransform transform)
        {
            return new Pose3
            {
                Orientation = transform.q,
                Position = transform.p,
            };
        }

        public static PxTransform ToPxTransform(this Pose3 pose)
        {
            return new PxTransform
            {
                q = pose.Orientation,
                p = pose.Position,
            };
        }
    }
}
