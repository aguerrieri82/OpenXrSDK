﻿#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using OpenXr.Framework.Android;
using OpenXr.Framework.Vulkan;
using OpenXr.Framework;
using XrEngine.Filament;
using XrEngine.OpenGL;
using OpenXr.Framework.Oculus;
using Microsoft.Extensions.Logging;
using Context2 = global::Android.Content.Context;

namespace XrEngine.OpenXr.Android
{
    public class AndroidPlatform : IXrEnginePlatform
    {
        readonly Context2 _context;
        private readonly DeviceInfo _info;
        VulkanDevice? _vkDevice;

        public AndroidPlatform(Context2 context)
        {
            PbrV1Material.LinearOutput = false;
            Context.Implement<IAssetStore>(new AndroidAssetStore(context, ""));
            Context.Implement<ILogger>(new AndroidLogger("XrApp"));
            Context.Implement<IProgressLogger>(new AndroidProgressLogger());
            Context.Implement<ITimeLogger>(NullTimeLogger.Instance);

            _context = context;

            _info = new DeviceInfo
            {
                Id = global::Android.Provider.Settings.Secure.GetString(context.ContentResolver, global::Android.Provider.Settings.Secure.AndroidId),
                Name = global::Android.OS.Build.Model,
            };
        }

        public XrApp CreateXrApp(IXrGraphicDriver xrDriver)
        {
            return new XrApp(Context.Require<ILogger>(),
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
            else
            {
                var glDriver = new AndroidXrOpenGLESGraphicDriver();

                var glOptions = options.DriverOptions as GlRenderOptions ?? new GlRenderOptions();

                renderEngine = new OpenGLRender(glDriver.GetApi<GL>(), glOptions);

                xrDriver = glDriver;
            }
        }

        public string Name => "Android";

        public string PersistentPath => _context.GetExternalFilesDir(null)!.AbsolutePath;

        public string CachePath => _context.CacheDir!.AbsolutePath;

        public string SharedPath => global::Android.OS.Environment.ExternalStorageDirectory!.AbsolutePath;

        public DeviceInfo Device => _info;
    }
}
