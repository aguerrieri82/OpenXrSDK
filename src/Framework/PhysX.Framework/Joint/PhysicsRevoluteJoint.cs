using System.Runtime.CompilerServices;

namespace PhysX.Framework
{
    public unsafe class PhysicsRevoluteJoint : PhysicsJoint
    {

        internal protected PhysicsRevoluteJoint(PxRevoluteJoint* handle, PhysicsSystem system)
            : base((PxJoint*)handle, system)
        {

        }

        public PxJointAngularLimitPair Limit
        {
            get => RevoluteJoint.GetLimit();
            set => RevoluteJoint.SetLimitMut(&value);
        }

        public float DriveVelocity
        {
            get => RevoluteJoint.GetDriveVelocity();
            set => RevoluteJoint.SetDriveVelocityMut(value, true);
        }


        public float DriveForceLimit
        {
            get => RevoluteJoint.GetDriveForceLimit();
            set => RevoluteJoint.SetDriveForceLimitMut(value);
        }

        public float DriveGearRatio
        {
            get => RevoluteJoint.GetDriveGearRatio();
            set => RevoluteJoint.SetDriveGearRatioMut(value);
        }

        public PxRevoluteJointFlags RevoluteJointFlags
        {
            get => RevoluteJoint.GetRevoluteJointFlags();
            set => RevoluteJoint.SetRevoluteJointFlagsMut(value);
        }


        public float Angle => RevoluteJoint.GetAngle();

        public float Velocity => RevoluteJoint.GetVelocity();

        public ref PxRevoluteJoint RevoluteJoint => ref Unsafe.AsRef<PxRevoluteJoint>(_handle);

    }
}
