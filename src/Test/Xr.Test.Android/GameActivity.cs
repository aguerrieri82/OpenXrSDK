using Android.Content;
using Android.Content.PM;
using Android.Webkit;
using OpenXr.Framework;
using OpenXr.Framework.Android;
using OpenXr.Framework.Oculus;
using OpenXr.Framework.Vulkan;
using OpenXr.Samples;
using Org.Xmlpull.V1.Sax2;
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
        private VulkanDevice _device;
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

        protected unsafe override SampleXrApp CreateApp()
        {

            Platform.Current = new Platform
            {
                AssetManager = new AndroidAssetManager(this)
            };

            _game = SampleScenes.CreateSimpleScene(Platform.Current.AssetManager);

            _device = new VulkanDevice();
            _device.Initialize(
                ["VK_KHR_surface", "VK_KHR_android_surface", "VK_KHR_external_memory_capabilities", "VK_KHR_get_physical_device_properties2"], 
                ["VK_KHR_swapchain", "VK_KHR_external_memory", "VK_KHR_get_memory_requirements2"]
            );

            var ctx = new FilamentLib.VulkanSharedContext
            {
                GraphicsQueueFamilyIndex = _device.QueueFamilyIndex,
                GraphicsQueueIndex = _device.QueueIndex,
                Instance = _device.Instance.Handle,
                LogicalDevice = _device.LogicalDevice.Handle,
                PhysicalDevice = _device.PhysicalDevice.Handle
            };

            _game.Renderer = new FilamentRender(new FilamentOptions
            {
                Context = new(&ctx),
                Driver = FilamentLib.FlBackend.Vulkan,
                MaterialCachePath = GetExternalCacheDirs()![0].AbsolutePath,
                EnableStereo = true
            });


            _game.Renderer = new FilamentRender(new FilamentOptions
            {
                Context = (IntPtr)driver.Context.Context!.NativeHandle,
                Driver = FilamentLib.FlBackend.OpenGL,
                MaterialCachePath = GetExternalCacheDirs()![0].AbsolutePath,
                EnableStereo = true
            });
         


            var logger = new AndroidLogger("XrApp");

            var result = new SampleXrApp(logger,
                 new AndroidXrOpenGLESGraphicDriver(),
                 //new XrVulkanGraphicDriver(_device),
                 new OculusXrPlugin(),
                 new AndroidXrPlugin(this));

            result.RenderOptions.SampleCount = 1;
            result.RenderOptions.RenderMode = XrRenderMode.Stereo;

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


            var renderer = result.BindEngineApp(_game);

            if (renderer is OpenGLRender glRenderer)
                glRenderer.Options.RequireTextureCompression = true;

            // StartService(new Intent(this, typeof(WebLinkService)));

            return result;
        }
    }
}