using OpenXr.Framework;
using System.Numerics;
using XrEngine;
using XrEngine.Bullet;
using XrEngine.OpenXr;
using XrMath;

namespace XrSamples
{
    public static partial class SampleScenes
    {
        [Sample("IK")]
        public static XrEngineAppBuilder CreateIk(this XrEngineAppBuilder builder)
        {
            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            var sphere1 = new TriangleMesh(Sphere3D.Default,
                (Material)MaterialFactory.CreatePbr(new Color(1f, 0, 0, 1)))
            {
                Name = "right"
            };

            var sphere2 = new TriangleMesh(Sphere3D.Default,
                (Material)MaterialFactory.CreatePbr(new Color(1f, 0, 0, 1)))
            {
                Name = "left"
            };

            var sphere3 = new TriangleMesh(Sphere3D.Default,
                (Material)MaterialFactory.CreatePbr(new Color(1f, 1, 0, 1)))
            {
                Name = "head"
            };

            sphere3.SetWorldPose(new Pose3()
            {
                Position = new Vector3(0f, 1.4599999f, 0f),
                Orientation = new Quaternion(0f, 0f, 0f, 1f)
            });

            sphere2.SetWorldPose(new Pose3()
            {
                Position = new Vector3(-0.53f, 1.1999999f, 0f),
                Orientation = new Quaternion(0f, 0f, 0f, 1f)
            });

            sphere1.SetWorldPose(new Pose3()
            {
                Position = new Vector3(0.53f, 1.1999999f, 0f),
                Orientation = new Quaternion(0f, 0f, 0f, 1f)
            });

            var grp = new Group3D()
            {
                Name = "Preview"
            };

            sphere1.Transform.SetScale(0.05f);
            sphere2.Transform.SetScale(0.05f);
            sphere3.Transform.SetScale(0.05f);

            scene.AddChild(sphere1);
            scene.AddChild(sphere2);
            scene.AddChild(sphere3);
            scene.AddChild(grp);

            var solver = new IkSolver();
            solver.Build(IkBodies.CreateArms());

            var updated = grp.AddComponent<IkUpdater>();
            var viewer = grp.AddComponent<IkViewer>();

            updated.Solver = solver;
            viewer.Solver = solver;

            updated.SetTarget("Head", sphere3);
            updated.SetTarget("Hand-L", sphere2);
            updated.SetTarget("Hand-R", sphere1);

            return builder
                .UseApp(app)
                //.UseEnvironmentDepth()
                //.UseDefaultHDR()
                .ConfigureSampleApp()
                .ConfigureApp(a =>
                {
                    var left = a.Inputs!.Left!.GripPose!;
                    var right = a.Inputs!.Right!.GripPose!;

                    scene.AddBehavior((scene, ctx) =>
                    {
                        solver.WorldPose = grp.GetWorldPose();

                        if (XrApp.Current?.IsStarted == false)
                            return;

                        var head = XrApp.Current!.SpacesTracker.GetLastLocation(XrApp.Current.Head)!.Pose;
                        var ofs = new Vector3(0, 1.4f, 0);

                        var leftPos = (left.Value.Position - head.Position) + ofs;
                        var rightPos = (right.Value.Position - head.Position) + ofs;

                        sphere1.WorldPosition = rightPos;
                        sphere2.WorldPosition = leftPos;
                        sphere3.WorldPosition = ofs;
                    });
                });
        }
    }
}
