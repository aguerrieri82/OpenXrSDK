using Android.Content.PM;
using Android.OS;
using Android.Runtime;

namespace OpenXr.Framework.Android
{
    public abstract class XrActivity : Activity
    {
        private Thread? _loopThread;
        private XrApp? _xrApp;
        private bool _isExited;
        private readonly Handler _handler;
        protected string[] _permissions;

        public XrActivity()
        {
            _handler = new Handler(Looper.MainLooper!);
            _permissions = [
                "com.oculus.permission.USE_SCENE",
                "android.permission.WRITE_EXTERNAL_STORAGE",
                "android.permission.READ_EXTERNAL_STORAGE"
                ];
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

        protected override void OnDestroy()
        {
            _xrApp?.Dispose();

            _loopThread?.Join();

            Process.KillProcess(Process.MyPid());

            base.OnDestroy();
        }

        void RunAppThread()
        {
            _loopThread = new Thread(RunApp);
            _loopThread.Start();
        }

        void RunApp()
        {
            _xrApp = CreateApp();

            _xrApp.Start();

            _handler.Post(() => OnAppStarted(_xrApp));

            while (_xrApp.IsStarted)
                _xrApp.RenderFrame(_xrApp.Stage);

            _isExited = true;
            System.Diagnostics.Debug.WriteLine("---Run App exit---");

        }

        private void CheckPermissionsAndRun()
        {

            var toAsk = new List<string>();

            foreach (var permission in _permissions)
                if (CheckSelfPermission(permission) != Permission.Granted)
                    toAsk.Add(permission);

            if (toAsk.Count > 0)
                RequestPermissions([.. toAsk], 1);
            else
                RunAppThread();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            if (requestCode == 1 && grantResults.All(a => a == Permission.Granted))
                RunAppThread();

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}
