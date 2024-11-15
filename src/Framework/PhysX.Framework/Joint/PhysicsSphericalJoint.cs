using System.Numerics;
using System.Runtime.CompilerServices;
using XrMath;

namespace PhysX.Framework
{
    public unsafe class PhysicsSphericalJoint : PhysicsJoint
    {

        public PhysicsSphericalJoint(PxSphericalJoint* handle, PhysicsSystem system)
            : base((PxJoint*)handle, system)
        {
        }


        public PxSphericalJointFlags SphericalFlags
        {
            get => SphericalJoint.GetSphericalJointFlags();
            set => SphericalJoint.SetSphericalJointFlagsMut(value);
        }


        public PxJointLimitCone LimitCone
        {
            get => SphericalJoint.GetLimitCone();
            set => SphericalJoint.SetLimitConeMut(&value);
        }

        public float SwingZAngle => SphericalJoint.GetSwingZAngle();

        public float SwingYAngle => SphericalJoint.GetSwingYAngle();

        public ref PxSphericalJoint SphericalJoint => ref Unsafe.AsRef<PxSphericalJoint>(_handle);

    }
}
