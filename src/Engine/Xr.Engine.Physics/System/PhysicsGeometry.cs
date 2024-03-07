using MagicPhysX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static MagicPhysX.NativeMethods;

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

    public unsafe struct PhysicsGeometry : IDisposable
    {
        PxGeometryHolder* _holder;
        PhysicsGeometryType _type;

        public PhysicsGeometry(PxGeometry* handle, PhysicsGeometryType type)
        {
            _holder = (PxGeometryHolder*)Marshal.AllocHGlobal(sizeof(PxGeometryHolder));
            _holder->StoreAnyMut(handle);
            _type = type;   
        }

        public void Dispose()
        {
            if (_holder!= null)
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
