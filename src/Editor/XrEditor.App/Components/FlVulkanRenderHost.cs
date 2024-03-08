
using OpenXr.Framework;
using OpenXr.Framework.Vulkan;
using XrEngine;
using XrEngine.Filament;

namespace XrEditor.Components
{
    public class FlVulkanRenderHost : RenderHost, IXrGraphicProvider
    {
        FilamentLib.GraphicContextInfo.VulkanContext _vulkan;
        FilamentRender? _render;
        VulkanDevice _device;

        public unsafe override IRenderEngine CreateRenderEngine()
        {
            _device = new VulkanDevice();

            _device.Initialize(
                 ["VK_KHR_surface", "VK_KHR_external_memory_capabilities", "VK_KHR_win32_surface", "VK_KHR_external_fence_capabilities", "VK_KHR_external_semaphore_capabilities", "VK_KHR_get_physical_device_properties2"],
                 ["VK_KHR_swapchain", "VK_KHR_external_memory", "VK_KHR_external_memory_win32", "VK_KHR_external_fence", "VK_KHR_external_fence_win32", "VK_KHR_external_semaphore", "VK_KHR_external_semaphore_win32", "VK_KHR_get_memory_requirements2", "VK_KHR_dedicated_allocation"]
            );

            var ctx = new FilamentLib.VulkanSharedContext()
            {
                GraphicsQueueFamilyIndex = 0,
                GraphicsQueueIndex = 0,
                Instance = _device.Instance.Handle,
                LogicalDevice = _device.LogicalDevice.Handle,
                PhysicalDevice = _device.PhysicalDevice.Handle
            };

            _render = new FilamentRender(new FilamentOptions
            {
                WindowHandle = HWnd,
                Context = new nint(&ctx),
                Driver = FilamentLib.FlBackend.Vulkan,
                MaterialCachePath = "d:\\Materials",
                EnableStereo = false,
                OneViewPerTarget = false
            });

            return _render;
        }

        public IXrGraphicDriver CreateXrDriver()
        {
            return new XrVulkanGraphicDriver(_device);
        }

    }
}
