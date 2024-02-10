using Silk.NET.OpenXR;

namespace OpenXr.Framework
{
    public class XrSwapchainInfo
    {
        public Swapchain Swapchain;

        public Extent2Di Size;

        public NativeArray<SwapchainImageBaseHeader>? Images;

    }
}
