using Microsoft.Extensions.Logging;
using OpenXr.Framework;
using PhysX.Framework;
using Silk.NET.Vulkan;
using System.Diagnostics;
using System.Numerics;
using XrEngine;
using XrEngine.Audio;
using XrEngine.Colliders;
using XrEngine.OpenXr;
using XrEngine.Physics;
using XrMath;

namespace Xr.Test
{
    public class BallGenerator : Behavior<Scene>
    {
        readonly List<TriangleMesh> _balls = [];
        readonly DynamicSound _sound;

        public BallGenerator(DynamicSound sound)
        {
            _sound = sound;
            var pbrMat = PbrMaterial.CreateDefault();
            pbrMat.Color = new Color(1, 1, 0);
            pbrMat.MetallicRoughness!.MetallicFactor = 0;
            pbrMat.MetallicRoughness.RoughnessFactor = 0.3f;
            Material = pbrMat;
            //Material = new StandardMaterial() { Color = new Color(1, 1, 0) };
        }

        protected TriangleMesh NewBall()
        {
            var ball = new TriangleMesh(Sphere3D.Instance, Material);
            ball.Transform.SetScale(0.02f);
            ball.Name = "Ball " + _balls.Count;


            ball.AddComponent<BoundsGrabbable>();

            var emitter = ball.AddComponent<AudioEmitter>();

            var sc = ball.AddComponent<SphereCollider>();
            sc.Radius = 1;

            var rb = ball.AddComponent<RigidBody>();
            rb.BodyType = PhysicsActorType.Dynamic;
            rb.Material = new PhysicsMaterialInfo
            {
                Restitution = 0.5f,
                StaticFriction = 0.8f,
                DynamicFriction = 0.8f
            };

            var lastSpeed = Vector3.Zero;

            Object3D? lastContact = null;

            ball.AddBehavior((me, ctx) =>
            {
                if (me.LiveTime < 0.1f || rb.BodyType == PhysicsActorType.Static)
                    return;

                var ds = (rb.Actor.LinearVelocity - lastSpeed) / (float)ctx.DeltaTime;

                if (ds.Length() > 50)
                {
                    if (lastContact?.Name == "Racket")
                    {
                        var haptic = XrApp.Current!.Haptics["RightHaptic"];
                        haptic.VibrateStart(200, 1, TimeSpan.FromMilliseconds(50));
                        lastContact = null;
                    }

                    var force = MathUtils.MapRange(ds.Length(), 100, 500);
                    emitter.Play(_sound.Buffer(force), rb.Actor.LinearVelocity.Normalize());
                }

                lastSpeed = rb.Actor.LinearVelocity;
            });

            rb.Contact += (me, other, index, data) =>
            {
                lastContact = other;
            };


            return ball;
        }

        public Object3D PickBall()
        {
            var toDelete = _balls.Where(a => a.LiveTime > 0).ToArray();

            foreach (var item in toDelete)
            {
                item.Dispose();
                _balls.Remove(item);
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
