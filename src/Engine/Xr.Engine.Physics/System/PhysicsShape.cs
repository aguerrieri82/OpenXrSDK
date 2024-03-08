using MagicPhysX;
using System.Runtime.CompilerServices;
using System.Text;

namespace Xr.Engine.Physics
{
    public struct PhysicsShapeInfo
    {
        public PhysicsShapeInfo()
        {
            IsEsclusive = true;
            Flags = PxShapeFlags.Visualization | PxShapeFlags.SceneQueryShape | PxShapeFlags.SimulationShape;
        }

        public PhysicsGeometry Geometry;

        public PhysicsMaterial Material;

        public bool IsEsclusive;

        public PxShapeFlags Flags;
    }

    public unsafe struct PhysicsShape
    {
        PxShape* _handle;
        private string _name;

        public PhysicsShape(PxShape* handle)
        {
            _handle = handle;
            _name = string.Empty;

        }


        public void Release()
        {
            if (_handle != null)
            {
                _handle->ReleaseMut();
                _handle = null;
            }
        }

        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                var data = Encoding.UTF8.GetBytes(value);
                fixed (byte* pData = data)
                    _handle->SetNameMut(pData);
                _name = value;
            }
        }

        public PxTransform LocalPose
        {
            get => _handle->GetLocalPose();
            set
            {
                _handle->SetLocalPoseMut(&value);
            }
        }

        public bool IsValid => _handle != null;

        public ref PxShape Shape => ref Unsafe.AsRef<PxShape>(_handle);

        public static implicit operator PxShape*(PhysicsShape self) => self._handle;
    }
}
