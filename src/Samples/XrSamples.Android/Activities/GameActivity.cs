using Android.Content;
using Android.Content.PM;
using Android.Webkit;
using OpenXr.Framework;
using OpenXr.Framework.Android;
using OpenXr.Framework.Oculus;
using Silk.NET.OpenXR;
using System.Text.Json;
using XrEngine;
using XrEngine.OpenXr;
using XrEngine.OpenXr.Android;


namespace XrSamples.Android.Activities
{

    [IntentFilter(["android.intent.action.VIEW"],
        Categories = ["com.oculus.intent.category.VR", "android.intent.category.DEFAULT"])]
    [Activity(
    Theme = "@android:style/Theme.Black.NoTitleBar.Fullscreen",
    LaunchMode = LaunchMode.SingleTask,
    Exported = true,
    MainLauncher = false,
    HardwareAccelerated = true,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout | ConfigChanges.Orientation,
    ScreenOrientation = ScreenOrientation.Landscape)]
    public class GameActivity : XrEngineActivity
    {
        private WebView? _webView;
        private XrWebViewLayer? _webViewLayer;
        private GameSettings? _settings;

        protected override void OnLoad()
        {
            var settingsJson = Intent?.GetStringExtra("Settings");

            if (settingsJson == null)
            {
                var intent = new Intent(this, typeof(SelectActivity));
                StartActivity(intent);
                Finish();
                return;
            }

            _settings = JsonSerializer.Deserialize<GameSettings>(settingsJson);

            base.OnLoad();
        }

        protected override void OnXrAppStarted(XrApp app)
        {
            /*
            if (_engine?.App.Renderer is OpenGLRender openGL)
                openGL.EnableDebug();
            */

            app.Plugin<OculusXrPlugin>().UpdateFoveation(FoveationDynamicFB.DisabledFB, FoveationLevelFB.HighFB, 90f);

            _webViewLayer = _engine!.XrApp.Layers.List.OfType<XrWebViewLayer>().FirstOrDefault();

            if (_webViewLayer != null)
            {
                _webView = _webViewLayer.WebView!;
                _webView.LoadUrl("https://www.youtube.com");
            }

            base.OnXrAppStarted(app);
        }

        protected override void BuildApp(XrEngineAppBuilder builder)
        {
            var external = global::Android.OS.Environment.ExternalStorageDirectory!.AbsolutePath;
            XrEngine.Context.Implement<IAssetStore>(new LocalAssetStore(Path.Combine(external, "Assets")));

            builder.Options.Driver = _settings!.Driver;

            if (_settings.Driver == GraphicDriver.OpenGL)
                builder.UseOpenGL(opt =>
                {
                    opt.UseDepthPass = _settings.EnableDepthPass;
                });

            if (_settings.Driver == GraphicDriver.OpenGL && _settings.IsMultiView)
                builder.UseMultiView();

            builder.SetRenderQuality(1f, (uint)_settings.Msaa);

            builder.RemovePlaneGrid()
                   .AddWebBrowser(this, app => app.ActiveScene?.FindByName<TriangleMesh>("display"));

            if (_settings.UseSpaceWarp)
                builder.UseSpaceWarp();

            if (_settings.UsePbrV2)
                MaterialFactory.DefaultPbr = typeof(PbrV2Material);
            else
                MaterialFactory.DefaultPbr = typeof(PbrV1Material);

            SampleScenes.DefaultHDR = _settings.Hdri;

            var manager = XrEngine.Context.Require<SampleManager>();
            var sample = manager.GetSample(_settings.SampleName!);

            sample.Build!(builder);

        }
    }
}