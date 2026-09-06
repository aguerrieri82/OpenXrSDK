using System.Numerics;
using XrEngine;
using XrEngine.Helpers;
using XrEngine.OpenXr;
using XrMath;

namespace XrSamples
{
    public static partial class SampleScenes
    {
        [Sample("Teleport")]
        public static XrEngineAppBuilder CreateTeleport(this XrEngineAppBuilder builder)
        {
            var app = CreateBaseScene();
            var scene = app.ActiveScene!;
            scene.ActiveCamera!.BackgroundColor = "#7C93DB";

            var mat = MaterialFactory.CreatePbr("#ffffff");
            mat.ColorMap = TextureFactory.CreateChecker();
            mat.ColorMap.Transform = Matrix3x3.CreateScale(10, 10);
            var floor = new TriangleMesh(Quad3D.Default, (Material)mat);
            floor.Transform.SetScale(10, 10, 1);
            floor.Transform.Orientation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, -MathF.PI / 2);
            floor.AddComponent<TeleportTarget>();
            floor.Name = "Floor";

            var cube = new TriangleMesh(Cube3D.Default, (Material)mat);
            cube.Transform.SetScale(3, 3, 3);
            cube.WorldPosition = new Vector3(2, 0, 2);
            cube.AddComponent<TeleportTarget>();
            cube.Name = "Cube";

            var player = new TriangleMesh(Cube3D.Default, (Material)MaterialFactory.CreatePbr("#ff0000"));
            player.Transform.SetScale(0.3f, 1.7f, 0.3f);
            player.AddComponent(new XrPlayer
            {
                Height = 0f
            });
            player.Name = "Player";

            scene.AddChild(floor);
            scene.AddChild(player);
            scene.AddChild(cube);
            return builder
                .UseApp(app)
                .UseDefaultHDR()
                .ConfigureSampleApp()
                .UseTeleport(ControllerHand.Left, player)
                .ConfigureApp(e =>
                {
                    e.XrApp.UseLocalSpace = false;

                    var root = e.App.ActiveScene!.Children.OfType<XrRoot>().First();
                    root.LeftController!.SetWorldPose(new Pose3()
                    {
                        Position = new Vector3(0f, 0.22f, 0f),
                        Orientation = new Quaternion(0.47238404f, -0.19674662f, -0.10905845f, 0.8522032f)
                    });
                });
        }
    }
}
