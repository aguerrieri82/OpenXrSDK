using System.Runtime.CompilerServices;

namespace PhysX.Framework
{

    public struct PhysicsMaterialInfo
    {
        public float StaticFriction;

        public float DynamicFriction;

        public float Restitution;

        public bool ForceNew;
    }


    public unsafe class PhysicsMaterial : PhysicsObject<PxMaterial>
    {
        protected int _refCount;

        public PhysicsMaterial(PxMaterial* handle, PhysicsSystem system)
            : base(handle, system)
        {
            AddRef();
        }

        public float Damping
        {
            get => _handle->GetDamping();
            set => _handle->SetDampingMut(value);
        }

        public float DynamicFriction
        {
            get => _handle->GetDynamicFriction();
            set => _handle->SetDynamicFrictionMut(value);
        }

        public float Restitution
        {
            get => _handle->GetRestitution();
            set => _handle->SetRestitutionMut(value);
        }

        public float StaticFriction
        {
            get => _handle->GetStaticFriction();
            set => _handle->SetStaticFrictionMut(value);
        }

        public PxCombineMode FrictionCombineMode
        {
            get => _handle->GetFrictionCombineMode();
            set => _handle->SetFrictionCombineModeMut(value);
        }

        public PxMaterialFlags Flags
        {
            get => _handle->GetFlags();
            set => _handle->SetFlagsMut(value);
        }

        public override void Dispose()
        {
            if (!_system.IsDisposed)
                _system.DeleteObject(this);

            GC.SuppressFinalize(this);
        }

        public void Release()
        {
            _refCount--;
            if (_refCount == 0)
                Dispose();
        }

        internal void AddRef()
        {
            _refCount++;
        }

        public ref PxMaterial Material => ref Unsafe.AsRef<PxMaterial>(_handle);
    }
}
