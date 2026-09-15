using Silk.NET.OpenXR;

namespace OpenXr.Framework.Oculus
{
    public class FoavetionInfo
    {
        public bool Use { get; set; }

        public bool IsDynamic { get; set; }

        public FoveationLevelFB Level { get; set; }

        public float Offset { get; set; }
    }

    public class OculusOptions
    {
        public OculusOptions()
        {
            Foavetion = new FoavetionInfo()
            {
                Use = true,
                IsDynamic = true,
                Level = FoveationLevelFB.HighFB,
                Offset = 0,
            };
            UseHandsWideMotion = true;
            UseBothHandAndControllers = false;
            HandTrackingFrequency = HandTrackingFrequencyHintMETA.HighMeta;
            ColorSpace = ColorSpaceFB.Rec709FB;
        }

        public bool UseHandsWideMotion { get; set; }

        public FoavetionInfo? Foavetion { get; set; }

        public ColorSpaceFB ColorSpace { get; set; }

        public HandTrackingDataSourceEXT[]? HandDataSources { get; set; }

        public bool HandTrackingUnextrapolated { get; set; }

        public bool UseDynamicResolution { get; set; }

        public bool UseBothHandAndControllers { get; set; }

        public bool UseBodyTrack { get; set; }

        public HandTrackingFrequencyHintMETA HandTrackingFrequency { get; set; }

    }
}
