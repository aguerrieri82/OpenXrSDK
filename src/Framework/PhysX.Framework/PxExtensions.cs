﻿using XrMath;

namespace PhysX.Framework
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
