using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
