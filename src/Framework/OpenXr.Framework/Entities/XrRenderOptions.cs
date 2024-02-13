using Silk.NET.OpenXR;

namespace OpenXr.Framework
{
    public class XrRenderOptions
    {
        public Extent2Di Size { get; set; }

        public EnvironmentBlendMode BlendMode { get; set; }

        public uint SampleCount { get; set; }

        public long SwapChainFormat { get; set; }

    }
}
