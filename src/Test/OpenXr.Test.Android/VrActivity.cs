using Android.Content;
using Android.Content.PM;
using Android.Nfc;
using Android.Opengl;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Webkit;
using OpenXr.Engine;
using OpenXr.Framework;
using OpenXr.Framework.Android;
using OpenXr.Framework.Oculus;
using System.Numerics;


namespace OpenXr.Test.Android
{
    [IntentFilter(["com.oculus.intent.category.VR"])]
    [Activity(
        Label = "@string/app_name",
        Theme = "@android:style/Theme.Black.NoTitleBar.Fullscreen",
        LaunchMode = LaunchMode.SingleTask,
        Exported = true,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout | ConfigChanges.Orientation,
        ScreenOrientation = ScreenOrientation.Landscape)]
    [MetaData("com.samsung.android.vr.application.mode", Value = "vr_only")]
    public class VrActivity : Activity
    {
        const string TAG = nameof(VrActivity);

        const int REQUEST_CODE_PERMISSION_USE_SCENE = 1;

        private XrApp _xrApp;
        private Thread? _loopThread;

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
                    cube.Transform.Scale = new Vector3(0.2f, 0.2f, 0.2f);
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


        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            CheckPermissionsAndRun();
        }

        void RunApp()
        {
            _loopThread = new Thread(ExecuteApp);
            _loopThread.Start();
        }

        void ExecuteApp()
        {
            _xrApp = new XrApp(
                new AndroidXrOpenGLESGraphicDriver(OpenGLESContext.Create()),
                new OculusXrPlugin(),
                new AndroidXrPlugin(this, (uint)Process.MyTid()));

            _xrApp.StartEventLoop();

            _xrApp.Start();

            _xrApp.BindEngineApp(CreateScene());

            var handler = new Handler(Looper.MainLooper!);

            while (!IsDestroyed)
                _xrApp.RenderFrame(_xrApp.Stage);
        }


        private void CheckPermissionsAndRun()
        {
            var perm = "com.oculus.permission.USE_SCENE";

            if (CheckSelfPermission(perm) != Permission.Granted)
                RequestPermissions([perm], REQUEST_CODE_PERMISSION_USE_SCENE);
            else
                RunApp();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            if (requestCode == REQUEST_CODE_PERMISSION_USE_SCENE && grantResults[0] == Permission.Granted)
                RunApp();

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}