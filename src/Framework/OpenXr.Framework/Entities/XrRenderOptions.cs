using Silk.NET.OpenXR;

namespace OpenXr.Framework
{
    public enum XrRenderMode
    {
        SingleEye,
        MultiView,
        Stereo
    }

    public class XrRenderOptions
    {
        public XrRenderOptions()
        {
            ResolutionScale = 1;
            SampleCount = 1;
            RenderMode = XrRenderMode.SingleEye;
            GpuLevel = PerfSettingsLevelEXT.BoostExt;
            CpuLevel = PerfSettingsLevelEXT.BoostExt;
        }

        public Extent2Di Size { get; set; }

        public float ResolutionScale { get; set; }

        public EnvironmentBlendMode BlendMode { get; set; }

        public uint SampleCount { get; set; }

        public long SwapChainFormat { get; set; }

        public XrRenderMode RenderMode { get; set; }

        public PerfSettingsLevelEXT CpuLevel { get; set; }

        public PerfSettingsLevelEXT GpuLevel { get; set; }
    }
}
