using PhysX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine.Physics
{
    public unsafe abstract class PhysicsObject<T> : IDisposable where T: unmanaged
    {
        protected T* _handle;

        protected PhysicsSystem _system;

        public PhysicsObject(T* handle, PhysicsSystem system)
        {
            _handle = handle;
            _system = system;
        }

        public PhysicsSystem System => _system;

        public object? Tag { get; set; }

        public abstract void Dispose();

        public bool IsValid => _handle != null;


        public static implicit operator T*(PhysicsObject<T> self) => self._handle;
    }
}
