using Silk.NET.OpenXR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Framework
{
    public class XrSwapchainInfo
    {
        public Swapchain Swapchain { get; set; }

        public Extent2Di Size { get; set; }

        public NativeArray<SwapchainImageBaseHeader>? Images { get; set; }

    }
}
