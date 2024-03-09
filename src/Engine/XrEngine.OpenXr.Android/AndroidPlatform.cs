#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using Android.Content;
using OpenXr.Framework.Android;
using OpenXr.Framework.Vulkan;
using OpenXr.Framework;
using XrEngine.Filament;
using XrEngine.OpenGL;
using OpenXr.Framework.Oculus;

namespace XrEngine.OpenXr.Android
{
    public class AndroidPlatform : IXrPlatform
    {
        Context _context;
        VulkanDevice? _vkDevice;

        public AndroidPlatform(Context context)
        {
            AssetManager = new AndroidAssetManager(context, "Assets");
            _context = context; 
        }

        public XrApp CreateXrApp(IXrGraphicDriver xrDriver)
        {
            var logger = new AndroidLogger("XrApp");

            return new XrApp(logger,
                new OculusXrPlugin(),
                xrDriver,
                new AndroidXrPlugin(_context));
        }

        public unsafe void CreateDrivers(XrEngineAppOptions options, out IRenderEngine renderEngine, out IXrGraphicDriver xrDriver)
        {

            if (options.Driver == GraphicDriver.FilamentVulkan || options.Driver == GraphicDriver.FilamentOpenGL)
            {
                var filamentOptions = new FilamentOptions
                {
                    Driver = FilamentLib.FlBackend.Vulkan,
                    MaterialCachePath = _context.GetExternalCacheDirs()![0].AbsolutePath,
                    EnableStereo = options.RenderMode != XrRenderMode.SingleEye,
                    OneViewPerTarget = true,
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
            else
            {
                var glDriver = new AndroidXrOpenGLESGraphicDriver();

                renderEngine = new OpenGLRender(glDriver.GetApi<GL>(), new GlRenderOptions
                {
                    RequireTextureCompression = true,
                });

                xrDriver = glDriver;
            }
        }

        public IAssetManager AssetManager { get; }
    }
}
