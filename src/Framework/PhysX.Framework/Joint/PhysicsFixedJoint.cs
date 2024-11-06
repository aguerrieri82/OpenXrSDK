using System.Runtime.CompilerServices;

namespace PhysX.Framework
{
    public unsafe class PhysicsFixedJoint : PhysicsJoint
    {

        public PhysicsFixedJoint(PxFixedJoint* handle, PhysicsSystem system)
            : base((PxJoint*)handle, system)
        {

        }

        public ref PxFixedJoint FixedJoint => ref Unsafe.AsRef<PxFixedJoint>(_handle);
    }
}
