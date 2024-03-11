using PhysX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using XrMath;

namespace PhysX.Framework
{
    public struct PhysicsContactPoint
    {
        public Vector3 Position;

        public Vector3 Normal;

        public float Separation;

        public Vector3 Impulse;

    }

    public struct ContactPairVelocity
    {
        public Vector3 Linear;

        public Vector3 Angular;
    }

    public struct ContactPairItem
    {
        public PhysicsShape? Shape;

        public Pose3 Pose;

        public ContactPairVelocity PostVelocity;

        public ContactPairVelocity PreVelocity;
    }

    public struct ContactPair
    {
        public ContactPairItem GetItem(int index)
        {
            return index == 0 ? Item0 : Item1;
        }

        public ContactPairItem Item0;

        public ContactPairItem Item1;

        public PhysicsShape? Shape2;

        public IList<PhysicsContactPoint>? Points;

    }
}
