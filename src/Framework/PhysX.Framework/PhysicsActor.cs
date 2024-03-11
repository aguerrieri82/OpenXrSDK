using PhysX;
using System.Numerics;
using System.Runtime.CompilerServices;
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


    public unsafe class PhysicsActor : PhysicsObject<PxActor>
    {
        internal PhysicsActor(PxActor* handle, PhysicsSystem system)
            : base(handle, system)
        { 
        }

        public void AddForce(Vector3 force, PxForceMode mode)
        {
            RigidBody.AddForceMut((PxVec3*)&force, mode, true);
        }

        public void AddForce(Vector3 force, Vector3 worldPos, PxForceMode mode)
        {
            RigidBody.ExtAddForceAtPos((PxVec3*)&force, (PxVec3*)&worldPos, mode, true);
        }

        public void Stop()
        { 
            LinearVelocity = Vector3.Zero;
            AngularVelocity = Vector3.Zero;
        }

        protected virtual internal void OnContact(PhysicsActor other, int otherIndex, ContactPair[] data)
        {
            Contact?.Invoke(other, otherIndex, data);
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

        public float Mass
        {
            get => RigidBody.GetMass();
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

        public bool IsKinematic
        {
            get => (RigidBody.GetRigidBodyFlags() & PxRigidBodyFlags.Kinematic) == PxRigidBodyFlags.Kinematic;
            set => RigidBody.SetRigidBodyFlagMut(PxRigidBodyFlag.Kinematic, value);
        }

        public Pose3 KinematicTarget
        {
            get
            {
                PxTransform value;
                RigidDynamic.GetKinematicTarget(&value);
                return value.ToPose3();
            }
            set
            {
                var newValue = value.ToPxTransform();
                RigidDynamic.SetKinematicTargetMut(&newValue);
            }
        }

        public Vector3 AngularVelocity
        {
            get => RigidDynamic.GetAngularVelocity();
            set => RigidDynamic.SetAngularVelocityMut((PxVec3*)&value, true);
        }

        public Vector3 LinearVelocity
        {
            get => RigidDynamic.GetLinearVelocity();
            set => RigidDynamic.SetLinearVelocityMut((PxVec3*)&value, true);
        }


        public event ActorContactEventHandler? Contact;

        public bool NotifyContacts { get; set; }

        public uint Id { get; internal set; }

        public ref PxRigidDynamic RigidDynamic => ref Unsafe.AsRef<PxRigidDynamic>(_handle);

        public ref PxRigidStatic RigidStatic => ref Unsafe.AsRef<PxRigidStatic>(_handle);

        public ref PxRigidBody RigidBody => ref Unsafe.AsRef<PxRigidBody>(_handle);

        public ref PxRigidActor RigidActor => ref Unsafe.AsRef<PxRigidActor>(_handle);

        public ref PxActor Actor => ref Unsafe.AsRef<PxActor>(_handle);
    }
}
