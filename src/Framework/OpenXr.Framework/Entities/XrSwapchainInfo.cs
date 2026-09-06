using Common.Interop;
using Silk.NET.OpenXR;

namespace OpenXr.Framework
{
    public class XrSwapchainInfo : IDisposable
    {
        public Swapchain ColorSwapchain;

        public Swapchain DepthSwapchain;

        public Extent2Di ViewSize;

        public Extent2Di DepthSize;

        public NativeArray<SwapchainImageBaseHeader>? ColorImages;

        public NativeArray<SwapchainImageBaseHeader>? DepthImages;

        public void Dispose()
        {
            ColorImages?.Dispose();
            DepthImages?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
