using Microsoft.Extensions.Logging;
using OpenXr.Framework;
using System.Diagnostics;
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
            rb.Contact += (me, other, index, data) =>
            {
                var dt = rb.Actor.System.LastDeltaTime;
                var selfIndex = index == 0 ? 1 : 0;

                var maxLinearAcc = data.Select(a =>
                {
                    var self = a.GetItem(selfIndex);
                    return ((self.PostVelocity.Linear - self.PreVelocity.Linear) / dt).Length();
                }).Max();
               
                var force = MathUtils.MapRange(maxLinearAcc, 8, 20);

                Platform.Current!.Logger.LogWarning("Acceleration: {acc} {force}", maxLinearAcc, force);

                if (maxLinearAcc > 5 || other.Name == "Racket")
                    emitter.Play(_sound.Buffer(force), data[0].Points![0].Normal);

                if (other.Name == "Racket")
                {
                    var haptic = XrApp.Current!.Haptics["RightHaptic"];
                    haptic.VibrateStart(100, 1, TimeSpan.FromMilliseconds(50));
                }
            };

            return ball;
        }

        public Object3D PickBall()
        {
            var toDelete = _balls.Where(a => a.LiveTime > 20).ToArray();

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
