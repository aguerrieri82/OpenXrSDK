using Silk.NET.Core.Native;
using Silk.NET.OpenXR;
using Silk.NET.OpenXR.Extensions.KHR;
using System.Text;

namespace OpenXr.Framework.Vulkan
{
    public unsafe class XrVulkanGraphicDriver : BaseXrPlugin, IXrGraphicDriver, IDisposable
    {
        protected IVulkanDevice _device;
        protected KhrVulkanEnable? _vulkanExt;
        protected XrApp? _app;
        protected XrDynamicType _swapChainType;

        public XrVulkanGraphicDriver(IVulkanDevice device)
        {
            _device = device;
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

        public long SelectSwapChainFormat(IList<long> availFormats)
        {
            return availFormats.First();
        }

        public GraphicsBinding CreateBinding()
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

            _device.Initialize(instExtensions, devExtensions);

            VkHandle physicalDevice;

            _app!.CheckResult(_vulkanExt.GetVulkanGraphicsDevice(_app.Instance, _app.SystemId, new VkHandle(_device.Instance.Handle), &physicalDevice), "GetVulkanGraphicsDeviceKHR");

            var binding = new GraphicsBinding();

            binding.VulkanKhr = new GraphicsBindingVulkanKHR()
            {
                Type = StructureType.GraphicsBindingVulkanKhr,
                Device = new VkHandle(_device.LogicalDevice.Handle),
                Instance = new VkHandle(_device.Instance.Handle),
                PhysicalDevice = physicalDevice,
                QueueFamilyIndex = 0,
                QueueIndex = 0,
            };

            return binding;
        }

        public XrDynamicType SwapChainImageType => _swapChainType;

        public void Dispose()
        {

            if (_device is IDisposable disposable)
                disposable.Dispose();
        }
    }
}
