using PhysX;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PhysX.Framework
{

    public unsafe class PhysicsGeometry : IDisposable
    {
        PxGeometryHolder* _holder;

        public PhysicsGeometry(PxGeometry* handle)
        {
            _holder = (PxGeometryHolder*)Marshal.AllocHGlobal(sizeof(PxGeometryHolder));
            _holder->StoreAnyMut(handle);
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

        public ref PxConvexMeshGeometry ConvexMesh => ref Unsafe.AsRef<PxConvexMeshGeometry>(_holder->ConvexMesh());

        public ref PxTriangleMeshGeometry TriangleMesh => ref Unsafe.AsRef<PxTriangleMeshGeometry>(_holder->TriangleMesh());

        public ref PxCapsuleGeometry Capsule => ref Unsafe.AsRef<PxCapsuleGeometry>(_holder->Capsule());

        public ref PxBoxGeometry Box => ref Unsafe.AsRef<PxBoxGeometry>(_holder->Box());

        public ref PxSphereGeometry Sphere => ref Unsafe.AsRef<PxSphereGeometry>(_holder->Sphere());

        public PxGeometryType Type => NativeMethods.PxGeometryHolder_getType(_holder);


        public static implicit operator PxGeometry*(PhysicsGeometry self) => self._holder->AnyMut();
    }
}
