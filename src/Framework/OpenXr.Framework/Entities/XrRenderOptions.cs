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
            UseProjectionDepth = false;
            UseQuodDepthCull = true;
        }

        public Extent2Di Size { get; set; }

        public float ResolutionScale { get; set; }

        public EnvironmentBlendMode BlendMode { get; set; }


        public uint SampleCount { get; set; }

        public long ColorFormat { get; set; }

        public long DepthFormat { get; set; }

        public XrRenderMode RenderMode { get; set; }

        public PerfSettingsLevelEXT CpuLevel { get; set; }

        public PerfSettingsLevelEXT GpuLevel { get; set; }

        public bool UseProjectionDepth { get; set; }

        public bool UseQuodDepthCull { get; set; }
    }
}
