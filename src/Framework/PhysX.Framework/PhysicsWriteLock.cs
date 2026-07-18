using System;
using System.Collections.Generic;
using System.Text;

namespace PhysX.Framework
{
    public unsafe struct PhysicsWriteLock : IDisposable
    {
        PxScene* _scene;

        public PhysicsWriteLock(PxScene* scene)
        {
            _scene = scene;
            _scene->LockWriteMut(null, 0);
        }

        public void Dispose()
        {
            _scene->UnlockWriteMut();
        }
    }
}
