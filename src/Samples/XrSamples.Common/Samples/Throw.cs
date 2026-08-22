using OpenXr.Framework;
using PhysX.Framework;
using XrEngine;
using XrEngine.OpenXr;
using XrEngine.Physics;

namespace XrSamples
{
    public static partial class SampleScenes
    {
        [Sample("Throw")]
        public static XrEngineAppBuilder CreateThrow(this XrEngineAppBuilder builder)
        {
            var settings = new ThrowSettings();
            var app = CreateBaseScene();
            var scene = app.ActiveScene!;

            scene.AddComponent<XrInputRecorder>();
            scene.AddComponent(new XrInputPlayer
            {
                RealTime = true,
                UseReferenceTime = true,
                FirstFrame = 300,
                LastFrame = 710,
                Loop = true
            });

            var cube = new TriangleMesh(Cube3D.Default, (Material)MaterialFactory.CreatePbr("#ff00000"));
            cube.Transform.SetScale(0.1f);
            cube.AddComponent<BoundsGrabbable>();
            cube.AddComponent<BoxCollider>();
            cube.AddComponent<Throwable>();

            var rb = cube.AddComponent(new RigidBody()
            {
                Type = PhysicsActorType.Dynamic,
                ToolMode = RigidBodyToolMode.KinematicTarget,
                AutoTeleport = false,
                Density = 100
            });

            scene.AddChild(cube);

            XrPoseInput? pose = null;
            XrBoolInput? pick = null;

            cube.AddBehavior((_, _) =>
            {
                if (XrApp.Current != null)
                {
                    pose ??= (XrPoseInput?)XrApp.Current!.Inputs["RightGripPose"];
                    pick ??= (XrBoolInput?)XrApp.Current!.Inputs["RightSqueezeClick"];
                }

                if (pick != null && pick.IsChanged && pick.Value)
                {
                    rb.Teleport(pose!.Value.Position);
                    //Context.Require<ITimeLogger>().Clear();
                }
            });

            return builder
              .UseApp(app)
              .ConfigureSampleApp()
              .UseDefaultHDR()
              .UsePhysics(new PhysicsOptions
              {

              })
              .AddPanel(new ThrowSettingsPanel(settings, cube))
              .ConfigureApp(app => settings.Apply(cube));
        }
    }
}
