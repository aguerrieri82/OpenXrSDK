using Android.Content.PM;
using Android.OS;
using OpenXr.Engine;
using OpenXr.Framework;
using OpenXr.Framework.Android;
using OpenXr.Framework.Oculus;
using System.Numerics;


namespace OpenXr.Test.Android
{

    [IntentFilter(["com.oculus.intent.category.VR"])]
    [Activity(
    Theme = "@android:style/Theme.Black.NoTitleBar.Fullscreen",
    LaunchMode = LaunchMode.SingleTask,
    Exported = true,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout | ConfigChanges.Orientation,
    ScreenOrientation = ScreenOrientation.Landscape)]
    [MetaData("com.samsung.android.vr.application.mode", Value = "vr_only")]
    public class GameActivity : XrActivity
    {
        public static EngineApp CreateScene()
        {
            var app = new EngineApp();

            var scene = new Scene();
            scene.ActiveCamera = new PerspectiveCamera() { Far = 10f, Near = 0.1f };

            var material = new StandardMaterial() { Color = new Color(1, 0, 0) };

            for (var y = -1f; y <= 1; y += 0.5f)
            {
                for (var rad = 0f; rad < Math.PI * 2; rad += MathF.PI / 10f)
                {
                    var x = MathF.Sin(rad) * 1;
                    var z = MathF.Cos(rad) * 1;

                    var cube = new Mesh(Cube.Instance, material);
                    cube.Transform.Scale = new Vector3(0.1f, 0.1f, 0.1f);
                    cube.Transform.Position = new Vector3(x, y, z);

                    cube.AddBehavior((obj, ctx) =>
                    {
                        obj.Transform.Orientation = Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0), (float)ctx.Time * MathF.PI / 4f);
                    });

                    scene.AddChild(cube);
                }
            }

            scene.AddChild(new AmbientLight(0.3f));
            scene.AddChild(new PointLight()).Transform.Position = new Vector3(0, 10, 10);

            app.OpenScene(scene);

            return app;
        }

        protected override XrApp CreateApp()
        {
            var result = new XrApp(
                 new AndroidXrOpenGLESGraphicDriver(OpenGLESContext.Create()),
                 new OculusXrPlugin(),
                 new AndroidXrPlugin(this, (uint)Process.MyTid()));

            result.BindEngineApp(CreateScene());

            return result;
        }
    }
}