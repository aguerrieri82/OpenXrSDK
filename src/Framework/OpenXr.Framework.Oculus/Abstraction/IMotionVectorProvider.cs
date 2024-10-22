using Silk.NET.OpenXR;

namespace OpenXr.Framework.Oculus
{
    public interface IMotionVectorProvider
    {
        unsafe void UpdateMotionVectors(ref Span<CompositionLayerProjectionView> projViews, SwapchainImageBaseHeader*[] colorImg, SwapchainImageBaseHeader*[] depthImg, XrRenderMode mode);

        public float Near { get; }

        public float Far { get; }

        public long MotionVectorFormat { get; }

        public long DepthFormat { get; }

        public bool IsActive { get; set; }
    }
}
