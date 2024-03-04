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

        public Extent2Di Size;

        public float ResolutionScale;

        public EnvironmentBlendMode BlendMode;

        public uint SampleCount;

        public long SwapChainFormat;

        public XrRenderMode RenderMode;

        public PerfSettingsLevelEXT CpuLevel;

        public PerfSettingsLevelEXT GpuLevel;
    }
}
