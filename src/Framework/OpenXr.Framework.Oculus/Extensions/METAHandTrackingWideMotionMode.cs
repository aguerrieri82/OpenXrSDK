using Silk.NET.OpenXR;

namespace OpenXr.Framework.Oculus
{

    public static class METAHandTrackingWideMotionMode
    {
        public const string ExtensionName = "XR_META_hand_tracking_wide_motion_mode";

        public const StructureType TypeHandTrackingWideMotionModeInfoMeta = (StructureType)1000539000;
    }

    public enum HandTrackingWideMotionModeMETA : int
    {
        HighFidelityBodyTrackingMeta = 1,
        MaxEnumMeta = 0x7FFFFFFF
    }

    public unsafe struct HandTrackingWideMotionModeInfoMETA
    {
        public HandTrackingWideMotionModeInfoMETA()
        {
            Type = METAHandTrackingWideMotionMode.TypeHandTrackingWideMotionModeInfoMeta;
        }

        public StructureType Type;

        public void* Next;

        public HandTrackingWideMotionModeMETA RequestedWideMotionMode;
    }
}
