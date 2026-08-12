#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using OpenXr.Framework.Android;
using OpenXr.Framework.Vulkan;
using OpenXr.Framework.Angle;
using OpenXr.Framework;
using XrEngine.Filament;
using XrEngine.OpenGL;
using Microsoft.Extensions.Logging;
using Context2 = global::Android.Content.Context;
using Silk.NET.OpenGLES.Extensions.EXT;

namespace XrEngine.OpenXr.Android
{
    public class AndroidPlatform : IXrEnginePlatform, IGlContextProvider
    {
        [ThreadStatic]
        internal static IGlContext? _currentGlContext;

#if GLES
        ExtClipControl? _clipControl;
#endif

        readonly Context2 _context;
        private readonly DeviceInfo _info;
        VulkanDevice? _vkDevice;

        public AndroidPlatform(Context2 context)
        {
            Context.Implement<IAssetStore>(new MergedAssetStore(
                new AndroidAssetStore(context, ""),
                new LocalAssetStore(Path.Combine(SharedPath, "Assets"))));

            Context.Implement<ILogger>(new AndroidLogger("XrApp"));
            Context.Implement<IProgressLogger>(new AndroidProgressLogger());
            Context.Implement<ITimeLogger>(NullTimeLogger.Instance);
            Context.Implement<IGlContextProvider>(this);

            _context = context;

            _info = new DeviceInfo
            {
                Id = global::Android.Provider.Settings.Secure.GetString(context.ContentResolver, global::Android.Provider.Settings.Secure.AndroidId),
                Name = global::Android.OS.Build.Model,
            };
        }

        public XrApp CreateXrApp(IList<IXrPlugin> plugins)
        {

            return new XrApp(Context.Require<ILogger>(), [..plugins, new AndroidXrPlugin(_context)]);
        }

        public unsafe void CreateDrivers(XrEngineAppOptions options, out IRenderEngine renderEngine, out IXrGraphicDriver xrDriver)
        {

            if (options.Driver == GraphicDriver.FilamentVulkan || options.Driver == GraphicDriver.FilamentOpenGL)
            {
                var filamentOptions = new FilamentOptions
                {
                    Driver = options.Driver == GraphicDriver.FilamentVulkan ? FilamentLib.FlBackend.Vulkan : FilamentLib.FlBackend.OpenGL,
                    MaterialCachePath = _context.GetExternalCacheDirs()![0].AbsolutePath,
                    EnableStereo = options.RenderMode != XrRenderMode.SingleEye,
                    OneViewPerTarget = false,
                    SampleCount = options.SampleCount
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

                    renderEngine = new FilamentRender(filamentOptions);

                    xrDriver = new XrVulkanGraphicDriver(_vkDevice);

                }
                else
                {
                    var glDriver = new AndroidXrOpenGLESGraphicDriver();

                    filamentOptions.Context = (IntPtr)glDriver.Context.Context!.NativeHandle;

                    renderEngine = new FilamentRender(filamentOptions);

                    xrDriver = glDriver;
                }
            }
            else if (options.Driver == GraphicDriver.Angle)
            {
                var glOptions = options.DriverOptions as GlRenderOptions ?? new GlRenderOptions();

                var ctx = new AngleVulkanContext();

                ctx.Initialize([], []);

                Context.Implement(ctx);

                var angleDriver = new XrAngleGraphicDriver(ctx);

                renderEngine = new OpenGLRender(ctx.Gl!, glOptions);

                if (_clipControl == null && !ctx.Gl!.TryGetExtension(out _clipControl))
                    throw new NotSupportedException();

                _clipControl!.ClipControl(EXT.UpperLeftExt, EXT.NegativeOneToOneExt);

                xrDriver = angleDriver;

                _currentGlContext = new AngleGlContext(ctx);
            }
            else
            {
                var glDriver = new AndroidXrOpenGLESGraphicDriver();

                var glOptions = options.DriverOptions as GlRenderOptions ?? new GlRenderOptions();

                var gl = glDriver.GetApi<GL>();

#if GL_WRAPPER

                renderEngine = new OpenGLRender(new OpenGLWrapper.GlSwitchWrapper(gl), glOptions);
#else
                renderEngine = new OpenGLRender(gl, glOptions);
#endif

                xrDriver = glDriver;

                _currentGlContext = new AndroidGlContext(glDriver.Context, gl);
            }
        }

        public IGlContext CreateShared()
        {
            if (_currentGlContext is AndroidGlContext androidGl)
                return androidGl.CreateShared(AndroidXrOpenGLESGraphicDriver.DEBUG_MODE);

            if (_currentGlContext is AngleGlContext angleGl)
            {
                var shared = ((AngleVulkanContext)angleGl.AngleContext).CreateSharedContext();
                return new AngleGlContext(shared);
            }

            throw new InvalidOperationException();
        }

        public string Name => "Android";

        public string PersistentPath => _context.GetExternalFilesDir(null)!.AbsolutePath;

        public string CachePath => _context.CacheDir!.AbsolutePath;

        public string SharedPath => global::Android.OS.Environment.ExternalStorageDirectory!.AbsolutePath;

        public DeviceInfo Device => _info;

        IGlContext? IGlContextProvider.Current => _currentGlContext;
    }
}
