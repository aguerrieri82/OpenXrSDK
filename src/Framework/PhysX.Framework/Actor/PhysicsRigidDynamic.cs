using System.Numerics;
using System.Runtime.CompilerServices;
using XrMath;

namespace PhysX.Framework
{
    public struct SolverIterations
    {
        public uint MinPos;

        public uint MinVel;
    }

    public unsafe class PhysicsRigidDynamic : PhysicsRigidBody
    {
        internal PhysicsRigidDynamic(PxActor* handle, PhysicsSystem system)
            : base(handle, system)
        {
        }

        public void Stop()
        {
            LinearVelocity = Vector3.Zero;
            AngularVelocity = Vector3.Zero;
            ClearForce(PxForceMode.Impulse);
            ClearTorque(PxForceMode.Impulse);
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
                RigidDynamic.GetSolverIterationCounts(&result.MinPos, &result.MinVel);
                return result;
            }
            set
            {
                RigidDynamic.SetSolverIterationCountsMut(value.MinPos, value.MinVel);
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

        protected internal ref PxRigidDynamic RigidDynamic => ref Unsafe.AsRef<PxRigidDynamic>(_handle);
    }
}
