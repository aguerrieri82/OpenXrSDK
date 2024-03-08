using OpenXr.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xr.Engine;
using Xr.Engine.Colliders;
using Xr.Engine.Physics;
using MagicPhysX;
using System.Numerics;
using Xr.Engine.OpenXr;

namespace Xr.Test
{
    public class BallGenerator : Behavior<Scene>
    {
        List<TriangleMesh> _balls = [];

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
                StaticFriction = 0.2f,
                DynamicFriction = 0.2f
            };

            return ball;
        }

        protected Object3D PickBall()
        {
            foreach (var ball in _balls)
            {
                if (ball.WorldPosition.Y < 10)
                {
                    var rb = ball.Components<RigidBody>().First();
                    rb.Actor.Stop();

                    return ball;
                }
            }

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
