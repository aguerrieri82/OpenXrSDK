﻿namespace PhysX.Framework
{
    public unsafe abstract class PhysicsObject<T> : IDisposable where T : unmanaged
    {
        protected T* _handle;

        protected PhysicsSystem _system;

        public PhysicsObject(T* handle, PhysicsSystem system)
        {
            _handle = handle;
            _system = system;
            _system.RegisterObject(handle, this);
        }

        public PhysicsSystem System => _system;

        public object? Tag { get; set; }

        public abstract void Dispose();

        public T* Handle => _handle;


        public static implicit operator T*(PhysicsObject<T> self) => self._handle;


    }
}
