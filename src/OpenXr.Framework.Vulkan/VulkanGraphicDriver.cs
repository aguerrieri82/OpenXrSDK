using Silk.NET.Core.Native;
using Silk.NET.OpenXR;
using Silk.NET.OpenXR.Extensions.KHR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Framework.Vulkan
{
    public unsafe class VulkanGraphicDriver : BaseXrPlugin, IXrGraphicDriver, IDisposable
    {
        NativeStruct<GraphicsBindingVulkanKHR> _binding;
        IVulkanDevice _device;
        KhrVulkanEnable? _vulkanExt;
        XrApp? _app;

        public VulkanGraphicDriver(IVulkanDevice device)
        {
            _device = device;
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

        public GraphicsBinding* CreateBinding()
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

            _binding.Value = new GraphicsBindingVulkanKHR()
            {
                Type = StructureType.GraphicsBindingVulkanKhr,
                Device = new VkHandle(_device.LogicalDevice.Handle),
                Instance = new VkHandle(_device.Instance.Handle),
                PhysicalDevice = physicalDevice,
                QueueFamilyIndex = 0,
                QueueIndex = 0,
            };

            return (GraphicsBinding*)_binding.Pointer;
        }

        public void Dispose()
        {
            _binding.Dispose();
            if (_device is IDisposable disposable)
                disposable.Dispose();
        }
    }
}
