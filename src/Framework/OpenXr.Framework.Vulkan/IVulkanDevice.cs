using Silk.NET.Vulkan;

namespace OpenXr.Framework.Vulkan
{
    public interface IVulkanDevice
    {

        void Initialize(string[] instanceExtensions, string[] deviceExtensions);

        PhysicalDevice PhysicalDevice { get; }

        Device LogicalDevice { get; }

        Instance Instance { get; }
    }
}
