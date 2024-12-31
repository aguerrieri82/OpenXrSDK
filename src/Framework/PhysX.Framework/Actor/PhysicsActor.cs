using System.Runtime.CompilerServices;
using System.Text;
using XrMath;

namespace PhysX.Framework
{
    public enum PhysicsActorType
    {
        Static,
        Dynamic,
        Kinematic
    }

    public struct PhysicsActorInfo
    {
        public PhysicsActorType Type;

        public IList<PhysicsShape> Shapes;

        public Pose3 Pose;

        public float Density;
    }


    public abstract unsafe class PhysicsActor : PhysicsObject<PxActor>
    {
        private string _name;

        internal PhysicsActor(PxActor* handle, PhysicsSystem system)
            : base(handle, system)
        {
            _name = string.Empty;
        }


        public override void Dispose()
        {
            if (!_system.IsDisposed)
            {
                _system.DeleteObject(this);

                if (_handle != null)
                {
                    _handle->ReleaseMut();
                    _handle = null;
                }
            }

            GC.SuppressFinalize(this);
        }


        public PxActorFlags ActorFlags
        {
            set => Actor.SetActorFlagsMut(value);
            get => Actor.GetActorFlags();
        }

        public string Name
        {
            get => _name;
            set
            {
                var data = Encoding.UTF8.GetBytes(value);
                fixed (byte* pData = data)
                    Actor.SetNameMut(pData);
                _name = value;
            }
        }

        public uint Id { get; internal set; }


        public ref PxActor Actor => ref Unsafe.AsRef<PxActor>(_handle);
    }
}
