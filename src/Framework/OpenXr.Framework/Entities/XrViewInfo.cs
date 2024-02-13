using Silk.NET.OpenXR;

namespace OpenXr.Framework
{
    public class XrViewInfo
    {
        public ViewConfigurationType Type;

        public bool FovMutable;

        public Extent2Di RecommendedImageRect;

        public Extent2Di MaxImageRect;

        public uint RecommendedSwapchainSampleCount;

        public uint MaxSwapchainSampleCount;

        public int ViewCount;

        public long[]? SwapChainFormats;

        public EnvironmentBlendMode[]? BlendModes;
    }
}
