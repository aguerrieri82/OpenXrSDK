using PhysX;
using System.Runtime.CompilerServices;
using System.Text;

namespace XrEngine.Physics
{
    public struct PhysicsShapeInfo
    {
        public PhysicsShapeInfo()
        {
            IsEsclusive = true;
            Flags = PxShapeFlags.Visualization | PxShapeFlags.SceneQueryShape | PxShapeFlags.SimulationShape;
        }

        public PhysicsGeometry? Geometry;

        public PhysicsMaterial? Material;

        public bool IsEsclusive;

        public PxShapeFlags Flags;
    }

    public unsafe class PhysicsShape : PhysicsObject<PxShape>
    {

        protected string _name;

        internal PhysicsShape(PxShape* handle, PhysicsSystem system)
    :            base(handle, system)
        {
            _name = string.Empty;
        }

        public override void Dispose()
        {
            _system._objects.Remove(new nint(_handle));

            if (_handle != null)
            {
                _handle->ReleaseMut();
                _handle = null;
            }
            GC.SuppressFinalize(this);
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


        public ref PxShape Shape => ref Unsafe.AsRef<PxShape>(_handle);
    }
}
