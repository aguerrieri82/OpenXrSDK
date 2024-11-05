using System.Runtime.CompilerServices;

namespace PhysX.Framework
{
    public unsafe class PhysicsRigidStatic : PhysicsRigidActor
    {
        internal PhysicsRigidStatic(PxActor* handle, PhysicsSystem system)
            : base(handle, system)
        {

        }

        protected internal ref PxRigidStatic RigidStatic => ref Unsafe.AsRef<PxRigidStatic>(_handle);
    }
}
