using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PhysX.Framework
{
    public unsafe class PhysicsStaticActor : PhysicsActor
    {
        internal PhysicsStaticActor(PxActor* handle, PhysicsSystem system)
            : base(handle, system)
        {
     
        }

        protected internal ref PxRigidStatic RigidStatic => ref Unsafe.AsRef<PxRigidStatic>(_handle);
    }
}
