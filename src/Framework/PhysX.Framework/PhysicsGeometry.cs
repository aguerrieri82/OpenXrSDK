using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using XrMath;
using static PhysX.NativeMethods;

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

        public unsafe RaycastHit[] Raycast(Ray3 ray, Pose3 pose, float maxDistance, PxHitFlags flags, uint maxHits)
        {
            var result = new PxGeomRaycastHit[maxHits];
            uint count;

            var transform = pose.ToPxTransform();

            fixed (PxGeomRaycastHit* pResult = result)
            {
                count = PxGeometryQuery_raycast(
                    (PxVec3*)&ray.Origin,
                    (PxVec3*)&ray.Direction,
                    this,
                    &transform,
                    maxDistance,
                    flags,
                    maxHits,
                    pResult,
                    0,
                    PxGeometryQueryFlags.SimdGuard,
                    null);
            }

            var newResults = new RaycastHit[count];

            for (var i = 0; i < count; i++)
            {
                newResults[i] = new RaycastHit
                {
                    Normal = result[i].normal,
                    Position = result[i].position,
                    UV = new Vector2(result[i].u, result[i].v),
                    Distance = result[i].distance
                };
            }

            return newResults;
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
