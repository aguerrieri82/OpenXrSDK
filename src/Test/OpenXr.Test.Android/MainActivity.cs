using Android.Content;
using Android.Content.PM;
using Android.Webkit;
using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using Silk.NET.OpenXR;
using Exception = System.Exception;



namespace OpenXr.Test.Android
{
    [IntentFilter([
        "android.intent.action.MAIN",
        "android.intent.category.LAUNCHER"])]
    [Activity(
        Label = "@string/app_name",
        Theme = "@android:style/Theme.Black.NoTitleBar.Fullscreen",
        ScreenOrientation = ScreenOrientation.Landscape,
        LaunchMode = LaunchMode.SingleTask,
        MainLauncher = true)]

    public class MainActivity : Activity
    {
        const string TAG = nameof(MainActivity);

        private WebView? _webView;

        protected unsafe override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            /*
            GlobalServices.App = new XrApp(
                //new XrVulkanGraphicDriver(new VulkanDevice()),
                new XrOpenGLESGraphicDriver(new OpenGLESDevice()),
                new OculusXrPlugin(),
                new OpenVrPlugin(this),
                new AndroidXrPlugin(this, (uint)Process.MyTid()));
            */

            SetContentView(Resource.Layout.activity_main);

            FindViewById<Button>(Resource.Id.getRooom)!.Click += (_, _) => _ = Task.Run(StartApp);

            StartService(new Intent(this, typeof(WebServerService)));

            ConfigureWebView();

            StartApp();
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

        protected override void OnDestroy()
        {
            StopService(new Intent(this, typeof(WebServerService)));
            base.OnDestroy();
        }

        private void StartApp()
        {
            StartActivity(new Intent(this, typeof(GameActivity)));
        }

        private async Task GetRoomAsync()
        {
            var app = GlobalServices.App!;

            app.Start();

            try
            {
                var xrOculus = app.Plugin<OculusXrPlugin>();

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
                        var local = app.LocateSpace(app.Stage, space.Space, 1);

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
                app.Stop();
            }
        }
    }
}