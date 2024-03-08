using OpenXr.Framework;
using XrEngine;
using XrEngine.Colliders;
using XrEngine.OpenXr;
using XrEngine.Physics;

namespace Xr.Test
{
    public class BallGenerator : Behavior<Scene>
    {
        readonly List<TriangleMesh> _balls = [];

        public BallGenerator()
        {
            Material = PbrMaterial.CreateDefault();
            Material.Color = new Color(1, 1, 0);
        }


        protected TriangleMesh NewBall()
        {
            var ball = new TriangleMesh(Sphere3D.Instance, Material);
            ball.Transform.SetScale(0.02f);
            ball.Name = "Ball " + _balls.Count;

            ball.AddComponent<BoundsGrabbable>();

            var sc = ball.AddComponent<SphereCollider>();
            sc.Radius = 1;

            var rb = ball.AddComponent<RigidBody>();
            rb.BodyType = PhysicsActorType.Dynamic;
            rb.Material = new PhysicsMaterialInfo
            {
                Restitution = 1,
                StaticFriction = 0.8f,
                DynamicFriction = 0.8f
            };

            return ball;
        }

        protected Object3D PickBall()
        {
            /*
            foreach (var ball in _balls)
            {
                if (ball.WorldPosition.Y < 10)
                {
                    var rb = ball.Components<RigidBody>().First();

                    rb.Actor.Stop();
                    rb.Actor.IsKinematic = true;
                    rb.Actor.KinematicTarget = new PxTransform
                    {
                        p = Pose!.Value.Position,
                        q = Quaternion.Identity
                    };
                    rb.Actor.IsKinematic = false;

                    return ball;
                }
            }
            */

            var newBall = NewBall();
            _balls.Add(newBall);
            _host!.Scene!.AddChild(newBall);
            return newBall;

        }

        protected override void Update(RenderContext ctx)
        {
            if (Generate == null && XrApp.Current != null)
            {
                Pose ??= (XrPoseInput?)XrApp.Current!.Inputs["LeftGripPose"];
                Generate ??= (XrBoolInput?)XrApp.Current!.Inputs["LeftTriggerClick"];
            }

            if (Generate != null && Generate.IsChanged && Generate.Value)
            {
                var ball = PickBall();
                ball.WorldPosition = Pose!.Value.Position;
            }

        }

        public Material? Material { get; set; }

        public XrPoseInput? Pose { get; set; }

        public XrBoolInput? Generate { get; set; }
    }
}
