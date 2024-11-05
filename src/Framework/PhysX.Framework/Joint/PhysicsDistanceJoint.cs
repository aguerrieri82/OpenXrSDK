using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XrMath;

namespace PhysX.Framework
{
    public unsafe class PhysicsDistanceJoint : PhysicsJoint
    {

        public PhysicsDistanceJoint(PxDistanceJoint* handle, PhysicsSystem system)
            : base((PxJoint*)handle, system)  
        {

        }

        public float MinDistance
        {
            get => DistanceJoint.GetMinDistance();
            set => DistanceJoint.SetMinDistanceMut(value);
        }

        public float MaxDistance
        {
            get => DistanceJoint.GetMaxDistance();
            set => DistanceJoint.SetMaxDistanceMut(value);
        }

        public float Tolerance
        {
            get => DistanceJoint.GetTolerance();
            set => DistanceJoint.SetToleranceMut(value);
        }

        public float Stiffness
        {
            get => DistanceJoint.GetStiffness();
            set => DistanceJoint.SetStiffnessMut(value);
        }

        public float Damping
        {
            get => DistanceJoint.GetDamping();
            set => DistanceJoint.SetDampingMut(value);
        }

        public float ContactDistance
        {
            get => DistanceJoint.GetContactDistance();
            set => DistanceJoint.SetContactDistanceMut(value);
        }

        public PxDistanceJointFlags DistanceJointFlags
        {
            get => DistanceJoint.GetDistanceJointFlags();
            set => DistanceJoint.SetDistanceJointFlagsMut(value);
        }

        public ref PxDistanceJoint DistanceJoint => ref Unsafe.AsRef<PxDistanceJoint>(_handle);

    }
}
