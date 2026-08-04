using Silk.NET.OpenXR;

namespace OpenXr.Framework.Oculus
{
    public interface IXrMotionVectorProvider
    {
        unsafe void UpdateMotionVectors(in SpaceWarpData spData, SwapchainImageBaseHeader* colorImg, SwapchainImageBaseHeader* depthImg, XrRenderMode mode);

        public float Near { get; }

        public float Far { get; }

        public int MotionVectorFormat { get; }

        public int DepthFormat { get; }

        public bool IsActive { get; set; }
    }
}
