using PhysX;
using System.Runtime.CompilerServices;

namespace PhysX.Framework
{

    public struct PhysicsMaterialInfo
    {
        public float StaticFriction;

        public float DynamicFriction;

        public float Restitution;
    }


    public unsafe class PhysicsMaterial : PhysicsObject<PxMaterial>
    {

        public PhysicsMaterial(PxMaterial* handle, PhysicsSystem system)
            : base(handle, system)
        {

        }

        public float Dumping
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
            GC.SuppressFinalize(this);
        }

        public ref PxMaterial Material => ref Unsafe.AsRef<PxMaterial>(_handle);
    }
}
