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

    public delegate void ActorContactEventHandler(PhysicsActor other, int otherIndex, ContactPair[] pairs);


    public abstract unsafe class PhysicsActor : PhysicsObject<PxActor>
    {
        private string _name;

        internal PhysicsActor(PxActor* handle, PhysicsSystem system)
            : base(handle, system)
        {
            _name = string.Empty;
        }

        public void AddShape(PhysicsShape shape)
        {
            RigidActor.AttachShapeMut(shape);
        }

        public void RemoveShape(PhysicsShape shape)
        {
            RigidActor.DetachShapeMut(shape, true);
        }

        public void SetScale(float value, bool scaleMass = true)
        {
            RigidActor.PhysPxScaleRigidActor(value, scaleMass);
        }

        public PhysicsShape[] GetShapes()
        {
            var size = RigidActor.GetNbShapes();

            var shapes = stackalloc PxShape*[(int)size];

            RigidActor.GetShapes(shapes, size, 0);

            var result = new PhysicsShape[size];

            for (var i = 0; i < size; i++)
                result[i] = _system.GetObject<PhysicsShape>(shapes[i]);

            return result;
        }

        protected virtual internal void OnContact(PhysicsActor other, int otherIndex, ContactPair[] data)
        {
            Contact?.Invoke(other, otherIndex, data);
        }

        public override void Dispose()
        {
            foreach (var shape in GetShapes())
                RemoveShape(shape);

            _system.DeleteObject(this);

            if (_handle != null)
            {
                _handle->ReleaseMut();
                _handle = null;
            }

            GC.SuppressFinalize(this);
        }

        public Pose3 GlobalPose
        {
            get => RigidActor.GetGlobalPose().ToPose3();
            set
            {
                var newValue = value.ToPxTransform();
                RigidActor.SetGlobalPoseMut(&newValue, true);
            }
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

        public bool NotifyContacts { get; set; }

        public uint Id { get; internal set; }


        public event ActorContactEventHandler? Contact;

        protected internal ref PxRigidActor RigidActor => ref Unsafe.AsRef<PxRigidActor>(_handle);

        public ref PxActor Actor => ref Unsafe.AsRef<PxActor>(_handle);
    }
}
