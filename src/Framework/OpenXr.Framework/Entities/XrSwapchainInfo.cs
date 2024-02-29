using Silk.NET.OpenXR;

namespace OpenXr.Framework
{
    public class XrSwapchainInfo
    {
        public Swapchain Swapchain;

        public Extent2Di ViewSize;

        public NativeArray<SwapchainImageBaseHeader>? Images;

    }
}
