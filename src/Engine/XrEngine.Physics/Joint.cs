using PhysX;
using PhysX.Framework;
using System.Numerics;
using XrMath;

namespace XrEngine.Physics
{
    public enum JointType
    {
        None,
        Distance,
        Revolute,
        Spherical,
        Fixed,
        D6
    }
    public class JointOptions
    {
        public JointOptions()
        {
            InvInertiaScale0 = 1;
            InvInertiaScale1 = 1;
            InvMassScale0 = 1;
            InvMassScale1 = 1;
        }

        public PxConstraintFlags ConstraintFlags;
        public float InvInertiaScale0;
        public float InvInertiaScale1;
        public float InvMassScale0;
        public float InvMassScale1;
        public string? Name;
    }

    public class RevoluteJointOptions : JointOptions
    {
        public RevoluteJointOptions()
        {
            DriveGearRatio = 1;
            DriveForceLimit = float.MaxValue;
        }

        public PxJointAngularLimitPair? Limit;
        public float DriveVelocity;
        public float DriveForceLimit;
        public float DriveGearRatio;
        public PxRevoluteJointFlags RevoluteJointFlags;
    }

    public class SphericalJointOptions : JointOptions
    {
        public SphericalJointOptions()
        {

        }

        public PxJointLimitCone? Limit;

        public PxSphericalJointFlags SphericalFlags;
    }

    public class D6JointOptions : JointOptions
    {
        public D6JointOptions()
        {
            ProjectionAngularTolerance = MathF.PI;
            ProjectionLinearTolerance = 1e10f;
        }

        public PxJointAngularLimitPair? TwistLimit;
        public PxJointLimitCone? SwingLimit;
        public PxJointLimitPyramid? PyramidSwingLimit;
        public PxJointLinearLimit? DistanceLimit;
        public float ProjectionAngularTolerance;
        public float ProjectionLinearTolerance;
        public Pose3? DrivePosition;
        public VelocityVector? DriveVelocity;
        public PxD6JointDrive? DriveX;
        public PxD6JointDrive? DriveY;
        public PxD6JointDrive? DriveZ;
        public PxD6JointDrive? DriveSwing;
        public PxD6JointDrive? DriveTwist;
        public PxD6JointDrive? DriveSlerp;
        public PxD6Motion MotionX;
        public PxD6Motion MotionY;
        public PxD6Motion MotionZ;
        public PxD6Motion MotionSwing1;
        public PxD6Motion MotionSwing2;
        public PxD6Motion MotionTwist;
    }

    public class Joint : IDisposable
    {
        PhysicsJoint? _joint;

        public Joint()
        {
        }

        internal void Create(RenderContext ctx)
        {
            if (Object0 == null || Object1 == null)
                return;

            var manager = Object0.Scene?.Component<PhysicsManager>();
            var system = manager?.System;
            if (system == null)
                throw new InvalidOperationException("Physics system is not initialized");

            var rb0 = Object0.Component<RigidBody>();
            var rb1 = Object1.Component<RigidBody>();

            rb0.EnsureCreated(ctx);
            rb1.EnsureCreated(ctx);

            if (Type == JointType.Distance)
            {
                _joint = system.CreateDistanceJoint(rb0.Actor, Pose0, rb1.Actor, Pose1);
            }
            else if (Type == JointType.Revolute)
            {
                _joint = system.CreateRevoluteJoint(rb0.Actor, Pose0, rb1.Actor, Pose1);
            }
            else if (Type == JointType.Fixed)
            {
                _joint = system.CreateFixedJoint(rb0.Actor, Pose0, rb1.Actor, Pose1);
            }
            else if (Type == JointType.D6)
            {
                _joint = system.CreateD6Joint(rb0.Actor, Pose0, rb1.Actor, Pose1);
            }
            else if (Type == JointType.Spherical)
            {
                _joint = system.CreateSphericalJoint(rb0.Actor, Pose0, rb1.Actor, Pose1);
            }

            Configure?.Invoke(this);

            UpdatePhysics();
        }

        protected void SetOptionsBase(JointOptions options)
        {
            _joint!.ConstraintFlags = options.ConstraintFlags;
            _joint.InvInertiaScale0 = options.InvInertiaScale0;
            _joint.InvInertiaScale1 = options.InvInertiaScale1;
            _joint.InvMassScale0 = options.InvMassScale0;
            _joint.InvMassScale1 = options.InvMassScale1;

            if (options.Name != null)
                _joint.Name = options.Name;

            if (options is RevoluteJointOptions rev)
                SetOptions(rev);

            else if (options is D6JointOptions d6)
                SetOptions(d6);

            else if (options is SphericalJointOptions sp)
                SetOptions(sp);
        }

        protected void SetOptions(SphericalJointOptions options)
        {
            var target = SphericalJoint;

            if (options.Limit != null)
                target.LimitCone = options.Limit.Value;

            target.SphericalFlags = options.SphericalFlags;
        }

        protected void SetOptions(RevoluteJointOptions options)
        {
            var target = RevoluteJoint;

            if (options.Limit != null)
                target.Limit = options.Limit.Value;

            target.DriveVelocity = options.DriveVelocity;
            target.DriveForceLimit = options.DriveForceLimit;
            target.DriveGearRatio = options.DriveGearRatio;
            target.RevoluteJointFlags = options.RevoluteJointFlags;
        }

        protected void SetOptions(D6JointOptions options)
        {
            var target = D6Joint;

            if (options.TwistLimit != null)
                target.TwistLimit = options.TwistLimit.Value;

            if (options.SwingLimit != null)
                target.SwingLimit = options.SwingLimit.Value;

            if (options.PyramidSwingLimit != null)
                target.PyramidSwingLimit = options.PyramidSwingLimit.Value;

            if (options.DistanceLimit != null)
                target.DistanceLimit = options.DistanceLimit.Value;

            target.ProjectionAngularTolerance = options.ProjectionAngularTolerance;
            target.ProjectionLinearTolerance = options.ProjectionLinearTolerance;

            if (options.DrivePosition != null)
                target.DrivePosition = options.DrivePosition.Value;

            if (options.DriveVelocity != null)
                target.DriveVelocity = options.DriveVelocity.Value;

            if (options.DriveX != null)
                target.DriveX = options.DriveX.Value;

            if (options.DriveY != null)
                target.DriveY = options.DriveY.Value;

            if (options.DriveZ != null)
                target.DriveZ = options.DriveZ.Value;

            if (options.DriveSwing != null)
                target.DriveSwing = options.DriveSwing.Value;

            if (options.DriveTwist != null)
                target.DriveTwist = options.DriveTwist.Value;

            if (options.DriveSlerp != null)
                target.DriveSlerp = options.DriveSlerp.Value;

            target.MotionX = options.MotionX;
            target.MotionY = options.MotionY;
            target.MotionZ = options.MotionZ;
            target.MotionSwing1 = options.MotionSwing1;
            target.MotionSwing2 = options.MotionSwing2;
            target.MotionTwist = options.MotionTwist;
        }

        public void UpdatePhysics()
        {
            if (_joint == null)
                return;

            if (Options != null)
                SetOptionsBase(Options);

            _joint.LocalPose0 = Pose0;
            _joint.LocalPose1 = Pose1;
        }

        public void Destroy()
        {
            if (_joint != null)
            {
                _joint.Dispose();
                _joint = null;
            }
        }

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }

        public PhysicsSphericalJoint SphericalJoint => _joint as PhysicsSphericalJoint ?? throw new InvalidOperationException("Joint is not a revolute joint");

        public PhysicsRevoluteJoint RevoluteJoint => _joint as PhysicsRevoluteJoint ?? throw new InvalidOperationException("Joint is not a revolute joint");

        public PhysicsFixedJoint FixedJoint => _joint as PhysicsFixedJoint ?? throw new InvalidOperationException("Joint is not a fixed joint");

        public PhysicsD6Joint D6Joint => _joint as PhysicsD6Joint ?? throw new InvalidOperationException("Joint is not a fixed joint");

        public PhysicsJoint BaseJoint => _joint ?? throw new InvalidOperationException("Joint is not a created");

        public bool IsCreated => _joint != null;

        public JointType Type { get; set; }

        public Object3D? Object0 { get; set; }

        public Object3D? Object1 { get; set; }

        public Pose3 Pose0 { get; set; }

        public Pose3 Pose1 { get; set; }

        public JointOptions? Options { get; set; }

        public Action<Joint>? Configure { get; set; }
    }
}
