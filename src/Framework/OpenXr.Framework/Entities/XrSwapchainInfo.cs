using Silk.NET.OpenXR;

namespace OpenXr.Framework
{
    public class XrSwapchainInfo
    {
        public Swapchain Swapchain { get; set; }

        public Extent2Di Size { get; set; }

        public NativeArray<SwapchainImageBaseHeader>? Images { get; set; }

    }
}
