using Silk.NET.OpenXR;

namespace OpenXr.Framework
{
    public class XrViewInfo
    {
        public ViewConfigurationType Type { get; set; }

        public bool FovMutable { get; set; }

        public Extent2Di RecommendedImageRect { get; set; }

        public Extent2Di MaxImageRect { get; set; }

        public uint RecommendedSwapchainSampleCount { get; set; }

        public uint MaxSwapchainSampleCount { get; set; }

        public int ViewCount { get; set; }

        public EnvironmentBlendMode[]? BlendModes { get; internal set; }
    }
}
