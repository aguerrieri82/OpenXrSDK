using Android.Content.PM;
using Android.Runtime;

namespace OpenXr.Framework.Android
{

    public abstract class XrActivity : Activity
    {
        private Thread? _loopThread;
        private XrApp? _xrApp;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            CheckPermissionsAndRun();
        }

        protected abstract XrApp CreateApp();


        protected virtual void OnAppStarted(XrApp app)
        {

        }

        void RunApp()
        {
            _loopThread = new Thread(ExecuteApp);
            _loopThread.Start();
        }

        void ExecuteApp()
        {
            _xrApp = CreateApp();

            _xrApp.StartEventLoop();

            _xrApp.Start();

            OnAppStarted(_xrApp);

            while (!IsDestroyed)
                _xrApp.RenderFrame(_xrApp.Stage);
        }


        private void CheckPermissionsAndRun()
        {
            var perm = "com.oculus.permission.USE_SCENE";

            if (CheckSelfPermission(perm) != Permission.Granted)
                RequestPermissions([perm], 1);
            else
                RunApp();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            if (requestCode == 1 && grantResults[0] == Permission.Granted)
                RunApp();

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}
