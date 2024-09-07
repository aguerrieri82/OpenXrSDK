using Android.Content;
using Android.Content.PM;
using Android.Webkit;
using OpenXr.Framework;
using OpenXr.Framework.Android;
using XrEngine;
using XrEngine.Media.Android;
using XrEngine.OpenGL;
using XrEngine.OpenXr;
using XrEngine.OpenXr.Android;
using XrEngine.Video;


namespace XrSamples.Android
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
    public class GameActivity : XrEngineActivity
    {
        private WebView? _webView;
        private XrWebViewLayer? _webViewLayer;

        protected override void OnAppStarted(XrApp app)
        {

            if (_engine?.App.Renderer is OpenGLRender openGL)
                openGL.EnableDebug();

            // app.Plugin<OculusXrPlugin>().UpdateFoveation(FoveationDynamicFB.DisabledFB, FoveationLevelFB.HighFB, 90f);

            _webViewLayer = _engine!.XrApp.Layers.List.OfType<XrWebViewLayer>().FirstOrDefault();

            if (_webViewLayer != null)
            {
                _webView = _webViewLayer.WebView!;
                _webView.LoadUrl("https://www.youtube.com");
            }

            base.OnAppStarted(app);
        }

        protected override void Build(XrEngineAppBuilder builder)
        {
            var ext = global::Android.OS.Environment.ExternalStorageDirectory!.AbsolutePath;
            XrEngine.Context.Implement<IAssetStore>(new LocalAssetStore(Path.Combine(ext, "Assets")));

            XrEngine.Context.Implement<IVideoReader>(() => new AndroidVideoReader());
            XrEngine.Context.Implement<IVideoCodec>(() => new AndroidVideoCodec());

            builder.UseOpenGL()
                    .UseFilamentOpenGL()
                   //.UseFilamentOpenGL()
                   //.UseStereo()
                   .UseMultiView()
                   .SetRenderQuality(1, 4)
                   .CreateBed()
                   .RemovePlaneGrid()
            .AddWebBrowser(this, "display");
        }
    }
}