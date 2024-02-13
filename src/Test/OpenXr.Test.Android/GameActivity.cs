using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Webkit;
using OpenXr.Engine;
using OpenXr.Framework;
using OpenXr.Framework.Android;
using OpenXr.Framework.Oculus;
using OpenXr.Samples;
using static Android.Renderscripts.ScriptGroup;



namespace OpenXr.Test.Android
{

    [IntentFilter([
        "com.oculus.intent.category.VR",
        "android.intent.action.MAIN",
        "android.intent.category.LAUNCHER"])]
    [Activity(
    Theme = "@android:style/Theme.Black.NoTitleBar.Fullscreen",
    LaunchMode = LaunchMode.SingleTask,
    Exported = true,
    MainLauncher = true,
    HardwareAccelerated = true,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout | ConfigChanges.Orientation,
    ScreenOrientation = ScreenOrientation.Landscape)]
    [MetaData("com.samsung.android.vr.application.mode", Value = "vr_only")]
    public class GameActivity : XrActivity
    {
        private EngineApp? _game;
        private WebView? _webView;
        private XrWebViewLayer? _webViewLayer;
        private XrMetaQuestTouchPro _inputs;

        protected override void OnAppStarted(XrApp app)
        {
            _webView = _webViewLayer!.WebView;
            _webView.LoadUrl("https://www.repubblica.it");
            //_webView!.LoadUrl("https://www.youtube.com/watch?v=wtdnatmVdIg");
            //_webView!.LoadUrl("https://www.eusoft.net/torrent/Movies/American.Assassin.2017.1080p.BluRay.x264.mp4");

            base.OnAppStarted(app);
        }

        protected override SampleXrApp CreateApp()
        {
            var options = new OculusXrPluginOptions
            {
                EnableMultiView = true,
                SampleCount = 2,
                ResolutionScale = 0.8f
            };

            _game = Common.CreateScene(new AndroidAssetManager(this)); 

            var logger = new AndroidLogger("XrApp");

            var result = new SampleXrApp(logger,
                 new AndroidXrOpenGLESGraphicDriver(),
                 new OculusXrPlugin(options),
                 new AndroidXrPlugin(this));

            result.Layers.Add<XrPassthroughLayer>();

            _webViewLayer = result.Layers.AddWebView(this,
                _game.ActiveScene!.FindByName<Mesh>("display")!.BindToQuad());

             _inputs = result.WithInteractionProfile<XrMetaQuestTouchPro>(bld => bld
                .AddAction(a => a.Right.GripPose)
                .AddAction(a => a.Right.GripAim)
                .AddAction(a => a.Right.TriggerClick));

            result.BindEngineApp(
                _game,
                options.SampleCount,
                options.EnableMultiView);

            // StartService(new Intent(this, typeof(WebLinkService)));

            return result;
        }
    }
}