using PhysX.Framework;
using System.Numerics;
using XrMath;

namespace XrEngine.Physics
{
    public enum JointType
    {
        None,
        Distance,
        Revolute
    }

    public class Joint : IDisposable   
    {
        PhysicsJoint? _joint;


        public Joint()
        {
            InvInertiaScale0 = 1;
            InvInertiaScale1 = 1;
            InvMassScale0 = 1;
            InvMassScale1 = 1;
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

            RevoluteJoint.ConstraintFlags |= PhysX.PxConstraintFlags.CollisionEnabled;

            UpdatePhysics();
        }

        public void UpdatePhysics()
        {
            if (_joint == null)
                return;

            if (_joint is PhysicsRevoluteJoint revoluteJoint)
            {
                var limit = revoluteJoint.Limit;

                limit.damping = Damping;
                limit.lower = -MathF.PI;
                limit.upper = MathF.PI;
                limit.bounceThreshold = BounceThreshold;
                limit.stiffness = Stiffness;
                limit.restitution = Restitution;
      
                revoluteJoint.RevoluteJointFlags |= PhysX.PxRevoluteJointFlags.LimitEnabled;

                revoluteJoint.Limit = limit;
            }

            _joint.InvInertiaScale0 = InvInertiaScale0;
            _joint.InvInertiaScale1 = InvInertiaScale1;
            _joint.InvMassScale0 = InvMassScale0;
            _joint.InvMassScale1 = InvMassScale1;

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


        public PhysicsRevoluteJoint RevoluteJoint => _joint as PhysicsRevoluteJoint ?? throw new InvalidOperationException("Joint is not a revolute joint");

        public JointType Type { get; set; } 

        public Object3D? Object0 { get; set; }

        public Object3D? Object1 { get; set; }

        public Pose3 Pose0 { get; set; }

        public Pose3 Pose1 { get; set; }    

        public float Damping { get; set; }    

        public float BounceThreshold { get; set; }

        public float Stiffness { get; set; }

        public float Restitution { get; set; }

        public float InvInertiaScale0 { get; set; }

        public float InvInertiaScale1 { get; set; }

        public float InvMassScale0 { get; set; }

        public float InvMassScale1 { get; set; }

    }
}
