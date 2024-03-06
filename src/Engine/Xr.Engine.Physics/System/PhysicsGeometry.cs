using MagicPhysX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Xr.Engine.Physics
{
    public enum PhysicsGeometryType
    {
        Box,
        Sphere,
        Capsule,
        TriangleMesh,
        ConvexMesh
    }

    public unsafe struct PhysicsGeometry
    {
        PxGeometryHolder _holder;
        PhysicsGeometryType _type;

        public PhysicsGeometry(PxGeometry* handle, PhysicsGeometryType type)
        {
            _holder.StoreAnyMut(handle);
            _type = type;   
        }

        public PhysicsGeometryType Type => _type;

        public ref PxGeometry Geometry => ref Unsafe.AsRef<PxGeometry>(_holder.Any());

        public static implicit operator PxGeometry*(PhysicsGeometry self) => self._holder.Any();
    }
}
