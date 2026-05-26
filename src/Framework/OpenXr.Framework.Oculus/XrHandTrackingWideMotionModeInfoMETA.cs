using Silk.NET.OpenXR;

namespace OpenXr.Framework.Oculus
{

    public static class MetaHandTrackingWideMotionMode
    {
        public const string ExtensionName = "XR_META_hand_tracking_wide_motion_mode";

        public const StructureType TypeHandTrackingWideMotionModeInfoMeta = (StructureType)1000539000;
    }


    public enum XrHandTrackingWideMotionModeMETA
    {
        HIGH_FIDELITY_BODY_TRACKING_META = 1,
        MAX_ENUM_META = 0x7FFFFFFF
    }


    public unsafe struct XrHandTrackingWideMotionModeInfoMETA
    {
        public StructureType Type;

        public void* Next;

        public XrHandTrackingWideMotionModeMETA RequestedWideMotionMode;
    }
}
