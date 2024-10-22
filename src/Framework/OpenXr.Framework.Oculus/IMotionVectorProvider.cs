using Silk.NET.OpenXR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Framework.Oculus
{
    public interface IMotionVectorProvider
    {
        unsafe void UpdateMotionVectors(ref Span<CompositionLayerProjectionView> projViews, SwapchainImageBaseHeader* colorImg, SwapchainImageBaseHeader* depthImg, XrRenderMode mode);    

        public float Near { get;  }

        public float Far { get; }

        public long MotionVectorFormat { get; }

        public long DepthFormat { get; }
    }
}
