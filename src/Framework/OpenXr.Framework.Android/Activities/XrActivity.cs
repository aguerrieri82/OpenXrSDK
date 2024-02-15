using Android.Content.PM;
using Android.Opengl;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Webkit;
using static Android.Renderscripts.Sampler;
using Debug = System.Diagnostics.Debug;

namespace OpenXr.Framework.Android
{

    public abstract class XrActivity : Activity
    {
        private Thread? _loopThread;
        private XrApp? _xrApp;
        private Handler _handler;

        public XrActivity()
        {
            _handler = new Handler(Looper.MainLooper!);
        }

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);


            CheckPermissionsAndRun();
        }

        protected abstract XrApp CreateApp();


        protected virtual void OnAppStarted(XrApp app)
        {

        }

        void RunAppThread()
        { 
            _loopThread = new Thread(RunApp);
            _loopThread.Start();
        }

        void RunApp()
        { 
            _xrApp = CreateApp();

            var driver = _xrApp.Plugin<AndroidXrOpenGLESGraphicDriver>();

            _xrApp.StartEventLoop(()=> IsDestroyed);

            _xrApp.Start();

            _handler.Post(() => OnAppStarted(_xrApp));

            while (!IsDestroyed)
                _xrApp.RenderFrame(_xrApp.Stage);

            _xrApp.Dispose();

        }

        private void CheckPermissionsAndRun()
        {
            var perm = "com.oculus.permission.USE_SCENE";

            if (CheckSelfPermission(perm) != Permission.Granted)
                RequestPermissions([perm], 1);
            else
                RunAppThread();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            if (requestCode == 1 && grantResults[0] == Permission.Granted)
                RunAppThread();

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}
