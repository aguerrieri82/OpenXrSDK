using MagicPhysX;
using System.Runtime.InteropServices;

namespace XrEngine.Physics
{
    public enum PhysicsGeometryType
    {
        Box,
        Sphere,
        Capsule,
        TriangleMesh,
        ConvexMesh
    }

    public unsafe struct PhysicsGeometry : IDisposable
    {
        PxGeometryHolder* _holder;
        readonly PhysicsGeometryType _type;

        public PhysicsGeometry(PxGeometry* handle, PhysicsGeometryType type)
        {
            _holder = (PxGeometryHolder*)Marshal.AllocHGlobal(sizeof(PxGeometryHolder));
            _holder->StoreAnyMut(handle);
            _type = type;
        }

        public void Dispose()
        {
            if (_holder != null)
            {
                Marshal.FreeHGlobal(new nint(_holder));
                _holder = null;
            }
            GC.SuppressFinalize(this);
        }

        public PhysicsGeometryType Type => _type;


        public static implicit operator PxGeometry*(PhysicsGeometry self) => self._holder->AnyMut();
    }
}
