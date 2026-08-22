using PhysX;
using PhysX.Framework;
using System.Numerics;
using XrEngine;
using XrEngine.Audio;
using XrEngine.Gltf;
using XrEngine.OpenXr;
using XrEngine.Physics;

namespace XrSamples
{
    public static partial class SampleScenes
    {
        [Sample("Ping Pong")]
        public static XrEngineAppBuilder CreatePingPong(this XrEngineAppBuilder builder)
        {
            var settings = new PingPongSettings();
            settings.Load(Path.Join(XrPlatform.Current!.PersistentPath, "settings.json"));

            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            var racket = (Group3D)GltfLoader.LoadFile(GetAssetPath("Paddle.glb"), GltfOptions);
            racket.Name = "Racket";

            //Reposition
            racket.Transform.LocalPivot = new Vector3(0.054f, -0.04f, 0.174f);
            racket.Transform.Update();
            racket.Transform.Rotation = new Vector3(-0.863f, -0.21f, -1.25f);
            racket.Transform.Position = Vector3.Zero;

            racket.Transform.Update();

            foreach (var geo in racket.DescendantsWithFeature<Geometry3D>())
                geo.Feature.ApplyTransform(racket.Transform.Matrix);

            racket.Transform.Reset();
            racket.Transform.Position = new Vector3(0, 1, 0);

            //Audio
            var audio = scene.Component<AudioSystem>();
            var sound = new DynamicSound();
            sound.AddBuffers(audio.Device.Al, Context.Require<IAssetStore>(), "BallSounds");

            //Grabber
            racket.AddComponent<BoundsGrabbable>();

            //Colliders
            foreach (var item in racket.DescendantsWithFeature<TriangleMesh>())
                racket.AddComponent(new MeshCollider(item.Feature.Geometry!));

            //Rigid body
            var rigidBody = racket.AddComponent<RigidBody>();
            rigidBody.Type = PhysicsActorType.Kinematic;
            rigidBody.MaterialInfo = new PhysicsMaterialInfo();

            //Ball generator
            var bg = scene!.AddComponent(new BallGenerator(sound, 0f));
            bg.PhysicSettings = settings.Ball;

            //Sample ball
            var ball = bg.PickBall(new Vector3(-0.5f, 1.1f, 0));

            var ballRigid = ball.Component<RigidBody>();
            ballRigid.Started += (_, _) =>
            {
                ballRigid.DynamicActor.AddForce(new Vector3(0.3f, 0, 0), PxForceMode.Force);
            };

            //Add racket
            scene!.AddChild(racket);

            //Setup camera
            scene.PerspectiveCamera.Target = racket.Transform.Position;

            return builder
                   .UseApp(app)
                   .UseSceneMesh(true, true)
                   .ConfigureSampleApp()
                   .UseDefaultHDR()
                   .SetGlOptions(opt =>
                   {
                       opt.ShadowMap.LightBleed = 1;
                       opt.ShadowMap.BlurRadius = 2;
                       opt.ShadowMap.Mode = ShadowMapMode.PCF;
                       opt.ShadowMap.UseFrustumIntersect = true;
                   })
                   //.AddFloorShadow()
                   .UsePhysics(new PhysicsOptions
                   {

                   })
                   .AddPanel(new PingPongSettingsPanel(settings, scene))
                   .ConfigureApp(app =>
                   {
                       settings.Apply(app.App.ActiveScene!);
                       scene.Children.OfType<SunLight>().Single().IsVisible = true;
                   });

        }
    }
}
