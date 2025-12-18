using System.Runtime.CompilerServices;
using XrMath;

namespace PhysX.Framework
{


    public delegate void ActorContactEventHandler(PhysicsActor other, int otherIndex, ContactPair[] pairs);


    public abstract unsafe class PhysicsRigidActor : PhysicsActor
    {
        private readonly string _name;

        internal PhysicsRigidActor(PxActor* handle, PhysicsSystem system)
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

        public IList<PhysicsShape> GetShapes()
        {
            var size = RigidActor.GetNbShapes();

            var shapes = stackalloc PxShape*[(int)size];

            RigidActor.GetShapes(shapes, size, 0);

            var result = new List<PhysicsShape>();
            for (var i = 0; i < size; i++)
            {
                var shape = _system.GetObject<PhysicsShape>(shapes[i]);
                if (shape != null)
                    result.Add(shape);
            }

            return result;
        }

        protected virtual internal void OnContact(PhysicsActor other, int otherIndex, ContactPair[] data)
        {
            Contact?.Invoke(other, otherIndex, data);
        }

        public override void Dispose()
        {
            if (!_system.IsDisposed)
            {
                foreach (var shape in GetShapes())
                    RemoveShape(shape);
            }

            base.Dispose();
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

        public bool NotifyContacts { get; set; }


        public event ActorContactEventHandler? Contact;

        protected internal ref PxRigidActor RigidActor => ref Unsafe.AsRef<PxRigidActor>(_handle);
    }
}
