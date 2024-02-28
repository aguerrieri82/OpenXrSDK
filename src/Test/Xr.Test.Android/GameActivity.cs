using Android.Content;
using Android.Content.PM;
using Android.Webkit;
using OpenXr.Framework;
using OpenXr.Framework.Android;
using OpenXr.Framework.Oculus;
using OpenXr.Samples;
using Xr.Engine;
using Xr.Engine.Filament;
using Xr.Engine.OpenGL;
using Xr.Engine.OpenXr;


namespace Xr.Test.Android
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
        private readonly XrWebViewLayer? _webViewLayer;
        private XrOculusTouchController? _inputs;

        protected override void OnAppStarted(XrApp app)
        {
            if (_webViewLayer != null)
            {
                _webView = _webViewLayer.WebView!;
                _webView.LoadUrl("https://www.youtube.com");
            }

            base.OnAppStarted(app);
        }

        protected override SampleXrApp CreateApp()
        {
            var options = new OculusXrPluginOptions
            {
                EnableMultiView = true,
                SampleCount = 1,
                ResolutionScale = 1f,
                Foveation = Silk.NET.OpenXR.SwapchainCreateFoveationFlagsFB.FragmentDensityMapBitFB
            };

            Platform.Current = new Platform
            {
                AssetManager = new AndroidAssetManager(this)
            };

            _game = SampleScenes.CreateSimpleScene(Platform.Current.AssetManager);

            var logger = new AndroidLogger("XrApp");

            var result = new SampleXrApp(logger,
                 new AndroidXrOpenGLESGraphicDriver(),
                 new OculusXrPlugin(options),
                 new AndroidXrPlugin(this));

            _inputs = result.WithInteractionProfile<XrOculusTouchController>(bld => bld
               .AddAction(a => a.Right!.TriggerClick)
               .AddAction(a => a.Right!.SqueezeClick)
               .AddAction(a => a.Right!.TriggerValue)
               .AddAction(a => a.Right!.SqueezeValue)
               .AddAction(a => a.Right!.GripPose)
               .AddAction(a => a.Right!.AimPose)
               .AddAction(a => a.Right!.Haptic)
              );

            result.Layers.Add<XrPassthroughLayer>();

            var display = _game.ActiveScene!.FindByName<TriangleMesh>("display")!;

            var controller = new SurfaceController(
                _inputs.Right!.TriggerClick!,
                _inputs.Right!.SqueezeClick!,
                _inputs.Right!.Haptic!);

            if (display != null)
            {
                display.AddComponent(controller);
                display.AddComponent<DisplayPosition>();

                //_webViewLayer = result.Layers.AddWebView(this, display.BindToQuad(), controller);
            }

            _game.ActiveScene!.AddComponent(new RayCollider(_inputs.Right!.AimPose!));
            _game.ActiveScene!.AddComponent(new ObjectGrabber(
                _inputs.Right!.GripPose!,
                _inputs.Right!.Haptic!,
                _inputs.Right!.SqueezeValue!,
                _inputs.Right!.TriggerValue!));

            //_game.ActiveScene.AddChild(new OculusSceneModel());

            var driver = result.Plugin<AndroidXrOpenGLESGraphicDriver>();

            /*
            _game.Renderer = new FilamentRender(new FilamentOptions
            {
                Context = (IntPtr)driver.Context.Context!.NativeHandle,
                Driver = FilamentLib.FlBackend.OpenGL,
                MaterialCachePath = GetExternalCacheDirs()![0].AbsolutePath
            });
            */


            var renderer = result.BindEngineApp(
                _game,
                options.SampleCount,
                options.EnableMultiView);

            if (renderer is OpenGLRender glRenderer)
                glRenderer.Options.RequireTextureCompression = true;

            // StartService(new Intent(this, typeof(WebLinkService)));

            return result;
        }
    }
}