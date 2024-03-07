using Android.Content;
using Android.Content.PM;
using Android.Webkit;
using OpenXr.Framework;
using OpenXr.Framework.Android;
using OpenXr.Framework.Oculus;
using OpenXr.Framework.Vulkan;
using OpenXr.Samples;
using Silk.NET.OpenGLES;
using Silk.NET.OpenXR;
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
        private VulkanDevice? _vkDevice;
        private WebView? _webView;
        private XrWebViewLayer? _webViewLayer;

        protected override void OnAppStarted(XrApp app)
        {
            var rates = app.Plugin<OculusXrPlugin>().EnumerateDisplayRefreshRates();

            app.Plugin<OculusXrPlugin>().UpdateFoveation(FoveationDynamicFB.DisabledFB, FoveationLevelFB.HighFB, 90f);

            if (_webViewLayer != null)
            {
                _webView = _webViewLayer.WebView!;
                _webView.LoadUrl("https://www.youtube.com");
            }

            base.OnAppStarted(app);
        }

        protected unsafe override XrApp CreateApp()
        {
            var renderMode = XrRenderMode.MultiView;
            var useFilament = false;
            uint sampleCount = 4;

            PbrMaterial.DefaultLinearOutput = true;

            Platform.Current = new Platform
            {
                AssetManager = new AndroidAssetManager(this, "Assets")
            };

            _game = SampleScenes.CreatePingPong(Platform.Current.AssetManager);

            IXrGraphicDriver driver;

            if (useFilament)
            {
                var filamentOptions = new FilamentOptions
                {
                    Driver = FilamentLib.FlBackend.Vulkan,
                    MaterialCachePath = GetExternalCacheDirs()![0].AbsolutePath,
                    EnableStereo = renderMode != XrRenderMode.SingleEye,
                    OneViewPerTarget = true
                };

                if (filamentOptions.Driver == FilamentLib.FlBackend.Vulkan)
                {
                    _vkDevice = new VulkanDevice();
                    _vkDevice.Initialize(
                        ["VK_KHR_surface", "VK_KHR_android_surface", "VK_KHR_external_memory_capabilities", "VK_KHR_get_physical_device_properties2"],
                        ["VK_KHR_swapchain", "VK_KHR_external_memory", "VK_KHR_get_memory_requirements2"]
                    );

                    var ctx = new FilamentLib.VulkanSharedContext
                    {
                        GraphicsQueueFamilyIndex = _vkDevice.QueueFamilyIndex,
                        GraphicsQueueIndex = _vkDevice.QueueIndex,
                        Instance = _vkDevice.Instance.Handle,
                        LogicalDevice = _vkDevice.LogicalDevice.Handle,
                        PhysicalDevice = _vkDevice.PhysicalDevice.Handle
                    };

                    filamentOptions.Context = new(&ctx);

                    _game.Renderer = new FilamentRender(filamentOptions);

                    driver = new XrVulkanGraphicDriver(_vkDevice);

                }
                else
                {
                    var glDriver = new AndroidXrOpenGLESGraphicDriver();

                    filamentOptions.Context = (IntPtr)glDriver.Context.Context!.NativeHandle;

                    _game.Renderer = new FilamentRender(filamentOptions);

                    driver = glDriver;
                }
            }
            else
            {
                var glDriver = new AndroidXrOpenGLESGraphicDriver();

                _game.Renderer = new OpenGLRender(glDriver.GetApi<GL>(), new GlRenderOptions
                {
                    RequireTextureCompression = true,
                });

                driver = glDriver;
            }

            var logger = new AndroidLogger("XrApp");

            var xrApp = new XrApp(logger, 
                new OculusXrPlugin(),
                driver, 
                new AndroidXrPlugin(this));

            xrApp.RenderOptions.SampleCount = sampleCount;
            xrApp.RenderOptions.RenderMode = renderMode;
            xrApp.RenderOptions.ResolutionScale = 0.8f;

            var inputs = SampleScenes.ConfigureXrApp(_game, xrApp);

            var display = _game.ActiveScene!.FindByName<TriangleMesh>("display")!;

            if (display != null)
            {
                var controller = new SurfaceController(
                    inputs.Right!.TriggerClick!,
                    inputs.Right!.SqueezeClick!,
                    inputs.Right!.Haptic!);

                display.AddComponent(controller);
                display.AddComponent<DisplayPosition>();

                _webViewLayer = xrApp.Layers.AddWebView(this, display.BindToQuad(), controller);
            }

            // StartService(new Intent(this, typeof(WebLinkService)));

            return xrApp;
        }
    }
}