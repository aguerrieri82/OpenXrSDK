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
            ColorScale = 1;
            SampleCount = 1;
            RenderMode = XrRenderMode.SingleEye;
            GpuLevel = PerfSettingsLevelEXT.BoostExt;
            CpuLevel = PerfSettingsLevelEXT.BoostExt;
            UseProjectionDepth = true;
            ProjectionDepthScale = 1f;
            BlendMode = EnvironmentBlendMode.Opaque;
            UseQuodDepthCull = false;
            UseSimmetricFov = true;
        }

        public Extent2Di Size { get; set; }

        public EnvironmentBlendMode BlendMode { get; set; }

        public uint SampleCount { get; set; }

        public int ColorFormat { get; set; }

        public int DepthFormat { get; set; }

        public XrRenderMode RenderMode { get; set; }

        public PerfSettingsLevelEXT CpuLevel { get; set; }

        public PerfSettingsLevelEXT GpuLevel { get; set; }

        public bool UseProjectionDepth { get; set; }

        public float ProjectionDepthScale { get; set; }

        public float ColorScale { get; set; }

        [Obsolete]
        public bool UseQuodDepthCull { get; set; }
        
        public bool UseSimmetricFov { get; set; }
    }
}
