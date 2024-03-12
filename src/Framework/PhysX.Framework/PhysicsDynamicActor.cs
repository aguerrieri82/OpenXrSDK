using System.Numerics;
using System.Runtime.CompilerServices;
using XrMath;

namespace PhysX.Framework
{
    public struct SolverIterations
    {
        public uint Min;

        public uint Max;
    }

    public unsafe class PhysicsDynamicActor : PhysicsActor
    {
        internal PhysicsDynamicActor(PxActor* handle, PhysicsSystem system)
            : base(handle, system)
        {
        }

        public void Stop()
        {
            LinearVelocity = Vector3.Zero;
            AngularVelocity = Vector3.Zero;
            CleaForce(PxForceMode.Impulse);
            ClearTorque(PxForceMode.Impulse);
        }


        public void CleaForce(PxForceMode mode)
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


        public float WakeCounter
        {
            get => RigidDynamic.GetWakeCounter();

            set => RigidDynamic.SetWakeCounterMut(value);
        }

        public PxRigidDynamicLockFlags LockFlags
        {
            get => RigidDynamic.GetRigidDynamicLockFlags();

            set => RigidDynamic.SetRigidDynamicLockFlagsMut(value);
        }


        public float ContactReportThreshold
        {
            get => RigidDynamic.GetContactReportThreshold();
            set => RigidDynamic.SetContactReportThresholdMut(value);
        }

        public float StabilizationThreshold
        {
            get => RigidDynamic.GetStabilizationThreshold();
            set => RigidDynamic.SetStabilizationThresholdMut(value);
        }

        public SolverIterations SolverIterations
        {
            get 
            {
                SolverIterations result;
                RigidDynamic.GetSolverIterationCounts(&result.Min, &result.Max); 
                return result;  
            }
            set
            {
                RigidDynamic.SetSolverIterationCountsMut(value.Min, value.Max);
            }
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

        public bool IsSleeping
        {
            get => RigidDynamic.IsSleeping();
            set
            {
                if (IsSleeping == value)
                    return;

                if (value)
                    RigidDynamic.PutToSleepMut();
                else
                    RigidDynamic.WakeUpMut();
            }
        }

        public float InvMass => RigidBody.GetInvMass();

        public float AngularDamping => RigidBody.GetAngularDamping();

        public float LinearDamping => RigidBody.GetLinearDamping();

        public Vector3 MassSpaceInvInertiaTensor => RigidBody.GetMassSpaceInvInertiaTensor();

        protected internal ref PxRigidBody RigidBody => ref Unsafe.AsRef<PxRigidBody>(_handle);

        protected internal ref PxRigidDynamic RigidDynamic => ref Unsafe.AsRef<PxRigidDynamic>(_handle);
    }
}
