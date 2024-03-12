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
        readonly float _maxTtl;

        public BallGenerator(DynamicSound sound, float maxTtl)
        {
            _sound = sound;
            _maxTtl = maxTtl;
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

            var audioEmitter = ball.AddComponent<AudioEmitter>();

            var collider = ball.AddComponent<SphereCollider>();
            collider.Radius = 1;

            var rigidBody = ball.AddComponent<RigidBody>();
            rigidBody.Type = PhysicsActorType.Dynamic;
            rigidBody.Material = new PhysicsMaterialInfo
            {
                Restitution = 0.5f,
                StaticFriction = 0.8f,
                DynamicFriction = 0.8f
            };

            rigidBody.Started += (_, _) =>
            {
                rigidBody.DynamicActor.ContactReportThreshold = 1f;
            };

            var lastSpeed = Vector3.Zero;

            Object3D? lastContact = null;
            Object3D? audioReceiver = null;

            ball.AddBehavior((me, ctx) =>
            {
                if (me.LifeTime < 0.1f || rigidBody.Type == PhysicsActorType.Static)
                    return;

                var deltaSpeed = (rigidBody.DynamicActor.LinearVelocity - lastSpeed) / (float)ctx.DeltaTime;

                if (deltaSpeed.Length() > 50)
                {
                    if (lastContact?.Name == "Racket")
                    {
                        var haptic = XrApp.Current!.Haptics["RightHaptic"];
                        haptic.VibrateStart(200, 1, TimeSpan.FromMilliseconds(50));
                        lastContact = null;
                    }

                    audioReceiver ??= _host!.ObjectsWithComponent<AudioReceiver>().FirstOrDefault();

                    if (audioReceiver != null)
                    {
                        var force = MathUtils.MapRange(deltaSpeed.Length(), 100, 500);

                        var dir = (ball.WorldPosition - audioReceiver.WorldPosition).Normalize();

                        audioEmitter.Play(_sound.Buffer(force), dir);
                    }
                }

                lastSpeed = rigidBody.DynamicActor.LinearVelocity;
            });

            rigidBody.Contact += (me, other, index, data) =>
            {
                lastContact = other;
            };

            return ball;
        }

        public Object3D PickBall(Vector3 worldPos)
        { 
            var expired = _balls.Where(a => a.LifeTime > _maxTtl);

            var ball = expired.FirstOrDefault();

            foreach (var item in expired.Skip(1))
            {
                item.Dispose();
                _balls.Remove(item);
            }

            if (ball != null)
            {
                var rigid = ball.Component<RigidBody>();
                rigid.DynamicActor.Stop();
                rigid.Teleport(worldPos);
            }
            else
            {
                ball = NewBall();
                ball.WorldPosition = worldPos;
                _balls.Add(ball);
                _host!.Scene!.AddChild(ball);
            }

            return ball;
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
                PickBall(Pose!.Value.Position);
            }
        }

        public Material? Material { get; set; }

        public XrPoseInput? Pose { get; set; }

        public XrBoolInput? Generate { get; set; }
    }
}
