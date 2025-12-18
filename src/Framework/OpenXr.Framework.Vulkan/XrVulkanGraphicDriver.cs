using Silk.NET.Core.Native;
using Silk.NET.OpenXR;
using Silk.NET.OpenXR.Extensions.KHR;
using Silk.NET.Vulkan;
using System.Text;
using StructureType = Silk.NET.OpenXR.StructureType;

namespace OpenXr.Framework.Vulkan
{
    public unsafe class XrVulkanGraphicDriver : XrBasePlugin, IXrGraphicDriver, IDisposable
    {
        protected IVulkanDevice _device;
        protected KhrVulkanEnable? _vulkanExt;
        protected XrDynamicType _swapChainType;

        protected Format[] _validFormats = [
            Format.R8G8B8A8Srgb,
            Format.R8G8B8A8Unorm];


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

        public override void SelectRenderOptions(XrViewInfo viewInfo, XrRenderOptions result)
        {
            result.ColorFormat = (long)_validFormats.First(a => viewInfo.SwapChainFormats!.Contains((long)a));
            result.DepthFormat = (long)Format.D24UnormS8Uint;
            //result.MotionVectorFormat = (long)Format.R16G16B16A16Sfloat;
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
                QueueFamilyIndex = _device.QueueFamilyIndex,
                QueueIndex = _device.QueueIndex,
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
