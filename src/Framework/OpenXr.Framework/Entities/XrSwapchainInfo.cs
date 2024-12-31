using Common.Interop;
using Silk.NET.OpenXR;

namespace OpenXr.Framework
{
    public class XrSwapchainInfo
    {
        public Swapchain ColorSwapchain;

        public Swapchain DepthSwapchain;

        public Extent2Di ViewSize;

        public NativeArray<SwapchainImageBaseHeader>? ColorImages;

        public NativeArray<SwapchainImageBaseHeader>? DepthImages;
    }
}
