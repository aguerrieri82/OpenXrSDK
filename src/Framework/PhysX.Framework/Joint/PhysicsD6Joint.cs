using System.Numerics;
using System.Runtime.CompilerServices;
using XrMath;

namespace PhysX.Framework
{
    public unsafe class PhysicsD6Joint : PhysicsJoint
    {

        public PhysicsD6Joint(PxD6Joint* handle, PhysicsSystem system)
            : base((PxJoint*)handle, system)
        {
  
        }

        public PxD6JointDrive GetDrive(PxD6Drive type)
        {
            return D6Joint.GetDrive(type);
        }

        public void SetDrive(PxD6Drive type, PxD6JointDrive value)
        {
            D6Joint.SetDriveMut(type, &value);
        }

        public void SetLinearLimit(PxD6Axis axis, PxJointLinearLimitPair value)
        {
            D6Joint.SetLinearLimitMut(axis, &value);
        }

        public PxJointLinearLimitPair GetLinearLimit(PxD6Axis axis)
        {
            return D6Joint.GetLinearLimit(axis);
        }

        public PxD6Motion GetMotion(PxD6Axis axis)
        {
            return D6Joint.GetMotion(axis);
        }

        public void SetMotion(PxD6Axis axis, PxD6Motion value)
        {
            D6Joint.SetMotionMut(axis, value);
        }

        public PxJointAngularLimitPair TwistLimit
        {
            get => D6Joint.GetTwistLimit();
            set => D6Joint.SetTwistLimitMut(&value);
        }

        public PxJointLimitCone SwingLimit
        {
            get => D6Joint.GetSwingLimit();
            set => D6Joint.SetSwingLimitMut(&value);
        }

        public PxJointLimitPyramid PyramidSwingLimit
        {
            get => D6Joint.GetPyramidSwingLimit();
            set => D6Joint.SetPyramidSwingLimitMut(&value);
        }

        public PxJointLinearLimit DistanceLimit
        {
            get => D6Joint.GetDistanceLimit();
            set => D6Joint.SetDistanceLimitMut(&value);
        }


        public float ProjectionAngularTolerance
        {
            get => D6Joint.GetProjectionAngularTolerance();
            set => D6Joint.SetProjectionAngularToleranceMut(value);
        }

        public float ProjectionLinearTolerance
        {
            get => D6Joint.GetProjectionLinearTolerance();
            set => D6Joint.SetProjectionLinearToleranceMut(value);
        }

        public Pose3 DrivePosition
        {
            get => D6Joint.GetDrivePosition().ToPose3();
            set
            {
                var tx = value.ToPxTransform();
                D6Joint.SetDrivePositionMut(&tx, true);
            }
        }

        public VelocityVector DriveVelocity
        {
            get
            {
                var result = new VelocityVector();
                D6Joint.GetDriveVelocity((PxVec3*)&result.Linear, (PxVec3*)&result.Angular);
                return result;
            }
            set
            {
                D6Joint.SetDriveVelocityMut((PxVec3*)&value.Linear, (PxVec3*)&value.Angular, true);
            }
        }



        public PxD6JointDrive DriveX
        {
            get => D6Joint.GetDrive(PxD6Drive.X);
            set => D6Joint.SetDriveMut(PxD6Drive.X, &value);
        }

        public PxD6JointDrive DriveY
        {
            get => D6Joint.GetDrive(PxD6Drive.Y);
            set => D6Joint.SetDriveMut(PxD6Drive.Y, &value);
        }

        public PxD6JointDrive DriveZ
        {
            get => D6Joint.GetDrive(PxD6Drive.Z);
            set => D6Joint.SetDriveMut(PxD6Drive.Z, &value);
        }

        public PxD6JointDrive DriveSwing
        {
            get => D6Joint.GetDrive(PxD6Drive.Swing);
            set => D6Joint.SetDriveMut(PxD6Drive.Swing, &value);
        }

        public PxD6JointDrive DriveTwist
        {
            get => D6Joint.GetDrive(PxD6Drive.Twist);
            set => D6Joint.SetDriveMut(PxD6Drive.Twist, &value);
        }

        public PxD6JointDrive DriveSlerp
        {
            get => D6Joint.GetDrive(PxD6Drive.Slerp);
            set => D6Joint.SetDriveMut(PxD6Drive.Slerp, &value);
        }

        public PxD6Motion MotionX
        {
            get => D6Joint.GetMotion(PxD6Axis.X);
            set => D6Joint.SetMotionMut(PxD6Axis.X, value);
        }

        public PxD6Motion MotionY
        {
            get => D6Joint.GetMotion(PxD6Axis.Y);
            set => D6Joint.SetMotionMut(PxD6Axis.Y, value);
        }

        public PxD6Motion MotionZ
        {
            get => D6Joint.GetMotion(PxD6Axis.Z);
            set => D6Joint.SetMotionMut(PxD6Axis.Z, value);
        }

        public PxD6Motion MotionSwing1
        {
            get => D6Joint.GetMotion(PxD6Axis.Swing1);
            set => D6Joint.SetMotionMut(PxD6Axis.Swing1, value);
        }

        public PxD6Motion MotionSwing2
        {
            get => D6Joint.GetMotion(PxD6Axis.Swing2);
            set => D6Joint.SetMotionMut(PxD6Axis.Swing2, value);
        }

        public PxD6Motion MotionTwist
        {
            get => D6Joint.GetMotion(PxD6Axis.Twist);
            set => D6Joint.SetMotionMut(PxD6Axis.Twist, value);
        }

        public float SwingZAngle => D6Joint.GetSwingZAngle();

        public float SwingYAngle => D6Joint.GetSwingYAngle();

        public float TwistAngle => D6Joint.GetTwistAngle();

        public ref PxD6Joint D6Joint => ref Unsafe.AsRef<PxD6Joint>(_handle);

    }
}
