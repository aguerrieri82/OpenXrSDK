using Silk.NET.Core.Native;
using Silk.NET.OpenXR;
using Silk.NET.OpenXR.Extensions.KHR;
using Silk.NET.Vulkan;
using System.Text;
using StructureType = Silk.NET.OpenXR.StructureType;
using Silk.NET.Core.Contexts;
using System.Diagnostics;



#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

namespace OpenXr.Framework.Angle
{


    public unsafe class XrAngleGraphicDriver : XrBasePlugin, IXrGraphicDriver, IDisposable
    {
        protected AngleVulkanContext _context;
        protected KhrVulkanEnable? _vulkanExt;
        protected XrDynamicType _swapChainType;

        protected GL? _gl;

        protected Format[] _validFormats = [
            Format.R8G8B8A8Srgb,
            Format.R8G8B8A8Unorm];

        public XrAngleGraphicDriver(AngleVulkanContext ctx)
        {
            _context = ctx;
            _swapChainType = new XrDynamicType
            {
                StructureType = StructureType.SwapchainImageVulkanKhr,
                Type = typeof(SwapchainImageVulkanKHR)
            };
        }

        public override void Initialize(XrApp app, IList<string> extensions)
        {
            _app = app;
            extensions.Add(KhrVulkanEnable.ExtensionName);

        }

        public override void OnInstanceCreated()
        {
            _app!.Xr.TryGetInstanceExtension<KhrVulkanEnable>(null, _app.Instance, out _vulkanExt);
        }

        public override void SelectRenderOptions(XrViewInfo viewInfo, XrRenderOptions result)
        {
            result.ColorFormat = (long)_validFormats.First(a => viewInfo.SwapChainFormats!.Contains((long)a));
            result.DepthFormat = (long)Format.D24UnormS8Uint;

        }

        public unsafe GraphicsBinding CreateBinding()
        {
            var vulkanReq = new GraphicsRequirementsVulkanKHR()
            {
                Type = StructureType.GraphicsRequirementsVulkanKhr
            };

            _app!.CheckResult(_vulkanExt!.GetVulkanGraphicsRequirements(_app!.Instance, _app.SystemId, &vulkanReq), "GetVulkanGraphicsRequirementsKHR");

            var buffer = new byte[2048];
            uint count = 0;

            _app!.CheckResult(_vulkanExt.GetVulkanDeviceExtension(_app.Instance, _app.SystemId, (uint)buffer.Length, ref count, ref buffer[0]), "GetVulkanDeviceExtensionsKHR");
            var devExtensions = Encoding.UTF8.GetString(buffer, 0, (int)count).Trim('\0').Split(' ');

            _app!.CheckResult(_vulkanExt.GetVulkanInstanceExtension(_app.Instance, _app.SystemId, (uint)buffer.Length, ref count, ref buffer[0]), "GetVulkanDeviceExtensionsKHR");

            var instExtensions = Encoding.UTF8.GetString(buffer, 0, (int)count).Trim('\0').Split(' ');

            _context.Initialize(instExtensions, devExtensions);

            VkHandle physicalDevice;

            _app!.CheckResult(_vulkanExt.GetVulkanGraphicsDevice(_app.Instance, _app.SystemId, 
                       new VkHandle(_context.VulkanInstanceHandle), &physicalDevice), "GetVulkanGraphicsDeviceKHR");

            var binding = new GraphicsBinding();

            binding.VulkanKhr = new GraphicsBindingVulkanKHR()
            {
                Type = StructureType.GraphicsBindingVulkanKhr,
                Device = new VkHandle(_context.VulkanDeviceHandle),
                Instance = new VkHandle(_context.VulkanInstanceHandle),
                PhysicalDevice = physicalDevice,
                QueueFamilyIndex = _context.QueueFamilyIndex,
                QueueIndex = 0,
            };

            _gl = GL.GetApi(_context);

         

            return binding;
        }
        
        public GL? Gl => _gl;

        public XrDynamicType SwapChainImageType => _swapChainType;

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
