using Android.Content.PM;
using Android.Nfc;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Webkit;
using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using OpenXr.Framework.Vulkan;
using OpenXr.WebLink;
using Silk.NET.OpenXR;


namespace OpenXr.Test.Android
{
    [IntentFilter(["com.oculus.intent.category.VR", "android.intent.action.MAIN", "android.intent.category.LAUNCHER"])]
    [Activity(
        Label = "@string/app_name",
        Theme = "@android:style/Theme.Black.NoTitleBar.Fullscreen",
        LaunchMode = LaunchMode.SingleTask,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout | ConfigChanges.Orientation,
        ScreenOrientation = ScreenOrientation.Landscape,
        MainLauncher = true)]
    public class MainActivity : Activity
    {
        const string TAG = "MainActivity";
        const string PERMISSION_USE_SCENE = "com.oculus.permission.USE_SCENE";
        const int REQUEST_CODE_PERMISSION_USE_SCENE = 1;


        XrApp? _app;
        private WebView? _webView;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.activity_main);

            FindViewById<Button>(Resource.Id.getRooom)!.Click += (_, _) => _= Task.Run(GetRoomAsync);

            RequestScenePermissionIfNeeded();
        }

        void ConfigureWebView()
        {

            WebView.SetWebContentsDebuggingEnabled(true);
            _webView = FindViewById<WebView>(Resource.Id.webView)!;

            _webView.Settings.JavaScriptEnabled = true;
            _webView.Settings.AllowContentAccess = true;
            _webView.Settings.DomStorageEnabled = true;
            _webView.Settings.JavaScriptCanOpenWindowsAutomatically = true;
            _webView.Settings.MixedContentMode = MixedContentHandling.AlwaysAllow;
            _webView.Settings.LoadsImagesAutomatically = true;

            _webView!.LoadUrl("https://roomdesigner.eusoft.net/");
        }

        private void RequestScenePermissionIfNeeded()
        {
            Log.Debug(TAG, "requestScenePermissionIfNeeded");
            if (CheckSelfPermission(PERMISSION_USE_SCENE) != Permission.Granted)
            {
                Log.Debug(TAG, "Permission has not been granted, request " + PERMISSION_USE_SCENE);
                RequestPermissions([PERMISSION_USE_SCENE], REQUEST_CODE_PERMISSION_USE_SCENE);
            }
        }


        private async Task GetRoomAsync()
        {
            if (_app == null)
            {
                _app = new XrApp(
                    new VulkanGraphicDriver(new VulkanDevice()),
                    new OculusXrPlugin(),
                    new AndroidXrPlugin(this));

                ProcessEvents();

                _app.Start();
                _app.WaitForSession(SessionState.Ready);
                _app.BeginSession(ViewConfigurationType.PrimaryStereo);
            }

          

            try
            {
                var xrOculus = _app.Plugin<OculusXrPlugin>();

                var anchors = await xrOculus.QueryAllAnchorsAsync().ConfigureAwait(true);

                foreach (var space in anchors)
                {
                    var components = xrOculus.GetSpaceSupportedComponents(space.Space);

                    if (components.Contains(SpaceComponentTypeFB.SemanticLabelsFB))
                    {
                        var label = xrOculus.GetSpaceSemanticLabels(space.Space);

                        Console.WriteLine(label[0]);
                    }

                    if (components.Contains(SpaceComponentTypeFB.RoomLayoutFB))
                    {
                        var roomLayout = xrOculus.GetSpaceRoomLayout(space.Space);

                        var walls = roomLayout.GetWalls();

                        Console.WriteLine(roomLayout);
                    }

                    if (components.Contains(SpaceComponentTypeFB.Bounded2DFB))
                    {
                        try
                        {
                            var bounds = xrOculus.GetSpaceBoundingBox2D(space.Space);
                            Console.WriteLine(bounds);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }

                    if (components.Contains(SpaceComponentTypeFB.LocatableFB))
                    {
                        var local = _app.LocateSpace(_app.Stage, space.Space, 1);

                        Console.WriteLine(local.Pose);
                    }

                    if (components.Contains(OculusXrPlugin.XR_SPACE_COMPONENT_TYPE_TRIANGLE_MESH_META))
                    {
                        var mesh = xrOculus.GetSpaceTriangleMesh(space.Space);
                        Console.WriteLine(mesh);
                    }
                }

            }
            finally
            {
                //_app.Stop();
            }
        }

        void ProcessEvents()
        {
            var handler = new Handler(Looper.MainLooper!);

            void ProcessWork()
            {
                if (_app != null)
                {
                    _app.HandleEvents();

                    try
                    {
                        var location = _app.LocateSpace(_app.Head, _app.Floor, 1);

                        var curPose = location.Pose.Convert().To<WebLink.Entities.Posef>();

                        Log.Debug("POSE", "{0}", curPose);

                    }
                    catch
                    {

                    }

                    handler.PostDelayed(ProcessWork, 50);
                }
            }

            handler.Post(ProcessWork);
        }

        static MainActivity()
        {
            Java.Lang.JavaSystem.LoadLibrary("openxr_loader");
        }
    }
}