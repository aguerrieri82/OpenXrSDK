using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

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
