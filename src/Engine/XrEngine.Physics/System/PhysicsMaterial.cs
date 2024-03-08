using MagicPhysX;
using System.Runtime.CompilerServices;

namespace XrEngine.Physics
{

    public struct PhysicsMaterialInfo
    {
        public float StaticFriction;

        public float DynamicFriction;

        public float Restitution;
    }


    public unsafe struct PhysicsMaterial
    {
        readonly PxMaterial* _handle;

        public PhysicsMaterial(PxMaterial* handle)
        {
            _handle = handle;
        }

        public bool IsValid => _handle != null;

        public ref PxMaterial Material => ref Unsafe.AsRef<PxMaterial>(_handle);

        public static implicit operator PxMaterial*(PhysicsMaterial self) => self._handle;
    }
}
