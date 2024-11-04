using System.Numerics;
using System.Runtime.CompilerServices;
using XrMath;

namespace PhysX.Framework
{

    public unsafe class PhysicsRigidBody : PhysicsRigidActor
    {
        internal PhysicsRigidBody(PxActor* handle, PhysicsSystem system)
            : base(handle, system)
        {
        }


        public void ClearForce(PxForceMode mode)
        {
            RigidBody.ClearForceMut(mode);
        }

        public void ClearTorque(PxForceMode mode)
        {
            RigidBody.ClearTorqueMut(mode);
        }

        public void AddTorque(Vector3 force, PxForceMode mode)
        {
            RigidBody.AddTorqueMut((PxVec3*)&force, mode, true);
        }

        public void AddForce(Vector3 force, PxForceMode mode)
        {
            RigidBody.AddForceMut((PxVec3*)&force, mode, true);
        }

        public void AddForce(Vector3 force, Vector3 worldPos, PxForceMode mode)
        {
            RigidBody.ExtAddForceAtPos((PxVec3*)&force, (PxVec3*)&worldPos, mode, true);
        }

        public void UpdateMassAndInertia(float density, Vector3 centerOfMassLocal)
        {
            RigidBody.ExtUpdateMassAndInertia1(density, (PxVec3*)&centerOfMassLocal, false);
        }

        public float Mass
        {
            get => RigidBody.GetMass();
            set => RigidBody.SetMassMut(value);
        }

        public float ContactSlopCoefficient
        {
            set => RigidBody.SetContactSlopCoefficientMut(value);
            get => RigidBody.GetContactSlopCoefficient();
        }

        public PxRigidBodyFlags RigidBodyFlags
        {
            set => RigidBody.SetRigidBodyFlagsMut(value);
            get => RigidBody.GetRigidBodyFlags();
        }

        public float MinCCDAdvanceCoefficient
        {
            set => RigidBody.SetMinCCDAdvanceCoefficientMut(value);
            get => RigidBody.GetMinCCDAdvanceCoefficient();
        }

        public float MaxAngularVelocity
        {
            set => RigidBody.SetMaxAngularVelocityMut(value);
            get => RigidBody.GetMaxAngularVelocity();
        }

        public float MaxLinearVelocity
        {
            set => RigidBody.SetMaxLinearVelocityMut(value);
            get => RigidBody.GetMaxLinearVelocity();
        }

        public float MaxDepenetrationVelocity
        {
            set => RigidBody.SetMaxDepenetrationVelocityMut(value);
            get => RigidBody.GetMaxDepenetrationVelocity();
        }

        public Vector3 MassSpaceInertiaTensor
        {
            get => RigidBody.GetMassSpaceInertiaTensor();
            set => RigidBody.SetMassSpaceInertiaTensorMut((PxVec3*)&value);
        }

        public bool IsKinematic
        {
            get => (RigidBody.GetRigidBodyFlags() & PxRigidBodyFlags.Kinematic) == PxRigidBodyFlags.Kinematic;
            set => RigidBody.SetRigidBodyFlagMut(PxRigidBodyFlag.Kinematic, value);
        }

        public float MaxContactImpulse
        {
            get => RigidBody.GetMaxContactImpulse();

            set => RigidBody.SetMaxContactImpulseMut(value);
        }

        public Pose3 CenterOfMassLocalPose
        {
            get => RigidBody.GetCMassLocalPose().ToPose3();

            set
            {

                var newValue = value.ToPxTransform();
                RigidBody.SetCMassLocalPoseMut(&newValue);
            }
        }


        public float AngularDamping
        {
            get => RigidBody.GetAngularDamping();

            set => RigidBody.SetAngularDampingMut(value);
        }

        public float LinearDamping
        {
            get => RigidBody.GetLinearDamping();

            set => RigidBody.SetLinearDampingMut(value);
        }

        public float InvMass => RigidBody.GetInvMass();

        public Vector3 MassSpaceInvInertiaTensor => RigidBody.GetMassSpaceInvInertiaTensor();

        protected internal ref PxRigidBody RigidBody => ref Unsafe.AsRef<PxRigidBody>(_handle);

    }
}
