using System.Runtime.CompilerServices;
using System.Text;
using XrMath;

namespace PhysX.Framework
{
    public unsafe abstract class PhysicsJoint : PhysicsObject<PxJoint>
    {
        protected string _name;
        protected internal PhysicsRigidActor? _actor0;
        protected internal PhysicsRigidActor? _actor1;

        public PhysicsJoint(PxJoint* handle, PhysicsSystem system)
            : base(handle, system)
        {
            _name = "";
        }

        public void SetActors(PhysicsRigidActor actor0, PhysicsRigidActor actor1)
        {
            _handle->SetActorsMut((PxRigidActor*)actor0.Handle, (PxRigidActor*)actor1.Handle);
            _actor0 = actor0;
            _actor1 = actor1;
        }

        public Pose3 LocalPose0
        {
            get => _handle->GetLocalPose(PxJointActorIndex.Actor0).ToPose3();
            set
            {
                var tr = value.ToPxTransform();
                _handle->SetLocalPoseMut(PxJointActorIndex.Actor0, &tr);
            }
        }

        public Pose3 LocalPose1
        {
            get => _handle->GetLocalPose(PxJointActorIndex.Actor1).ToPose3();
            set
            {
                var tr = value.ToPxTransform();
                _handle->SetLocalPoseMut(PxJointActorIndex.Actor1, &tr);
            }
        }

        public PxConstraintFlags ConstraintFlags
        {
            set => _handle->SetConstraintFlagsMut(value);
            get => _handle->GetConstraintFlags();
        }

        public float InvInertiaScale0
        {
            set => _handle->SetInvInertiaScale0Mut(value);
            get => _handle->GetInvInertiaScale0();
        }

        public float InvInertiaScale1
        {
            set => _handle->SetInvInertiaScale1Mut(value);
            get => _handle->GetInvInertiaScale1();
        }

        public float InvMassScale0
        {
            set => _handle->SetInvMassScale0Mut(value);
            get => _handle->GetInvMassScale0();
        }

        public float InvMassScale1
        {
            set => _handle->SetInvMassScale1Mut(value);
            get => _handle->GetInvMassScale1();
        }

        public string Name
        {
            get => _name;
            set
            {
                var data = Encoding.UTF8.GetBytes(value);
                fixed (byte* pData = data)
                    _handle->SetNameMut(pData);
                _name = value;
            }
        }
        public override void Dispose()
        {
            if (_handle != null)
            {
                _handle->ReleaseMut();
                _handle = null;
            }
        }

        public PhysicsRigidActor? Actor0 => _actor0;

        public PhysicsRigidActor? Actor1 => _actor1;

        public ref PxJoint Joint => ref Unsafe.AsRef<PxJoint>(_handle);
    }
}
