using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;


namespace OpenXr.Framework.Android
{
    public abstract class XrActivity : Activity
    {
        const int STORAGE_REQUEST = 100;
        const int PERMISSIONS_REQUEST = 100;

        [Flags]
        enum LoadStep
        {
            None,
            AskStorage,
            AskPermissions,
            Done,
        }

        private Thread? _loopThread;
        private XrApp? _xrApp;
        private bool _isExited;
        private readonly Handler _handler;
        protected string[] _permissions;
        private LoadStep _loadStep;

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

            NextLoadStep();
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

        private void CheckPermissions()
        {
            var toAsk = new List<string>();

            foreach (var permission in _permissions)
                if (CheckSelfPermission(permission) != Permission.Granted)
                    toAsk.Add(permission);

            if (toAsk.Count > 0)
                RequestPermissions([.. toAsk], PERMISSIONS_REQUEST);
            else
                NextLoadStep();
        }

        private void CheckStorage()
        {
            if (!global::Android.OS.Environment.IsExternalStorageManager)
            {
                var intent = new Intent(global::Android.Provider.Settings.ActionManageAppAllFilesAccessPermission!,
               global::Android.Net.Uri.Parse("package:" + Application.Context.PackageName));

                StartActivityForResult(intent, STORAGE_REQUEST);
            }
            else
                NextLoadStep();
        }

        private void NextLoadStep()
        {
            if (_loadStep == LoadStep.None)
            {
                _loadStep = LoadStep.AskPermissions;
                CheckPermissions();
            }
            else if (_loadStep == LoadStep.AskPermissions)
            {
                _loadStep = LoadStep.AskStorage;
                CheckStorage();
            }
            else if (_loadStep == LoadStep.AskStorage)
            {
                _loadStep = LoadStep.Done;
                RunAppThread();
            }

        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            if (requestCode == PERMISSIONS_REQUEST && grantResults.All(a => a == Permission.Granted))
                NextLoadStep();

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent? data)
        {
            if (requestCode == STORAGE_REQUEST)
            {
                NextLoadStep();
            }
            base.OnActivityResult(requestCode, resultCode, data);
        }
    }
}
