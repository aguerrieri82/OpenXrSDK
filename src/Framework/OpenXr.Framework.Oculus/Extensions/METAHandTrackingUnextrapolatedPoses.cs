using Silk.NET.OpenXR;

namespace OpenXr.Framework.Oculus
{
    public static class METAHandTrackingUnextrapolatedPoses
    {
        public const string ExtensionName = "XR_META_hand_tracking_unextrapolated_poses";

        public const StructureType TypeHandTrackingUnextrapolatedPosesRequestMeta = (StructureType)1000693000;

        public const StructureType TypeHandTrackingUnextrapolatedPosesMeta = (StructureType)1000693001;
    }

    public unsafe struct HandTrackingUnextrapolatedPosesRequestMETA
    {
        public HandTrackingUnextrapolatedPosesRequestMETA()
        {
            Type = METAHandTrackingUnextrapolatedPoses.TypeHandTrackingUnextrapolatedPosesRequestMeta;
        }

        public readonly StructureType Type;

        public void* Next;
    }

    public unsafe struct HandTrackingUnextrapolatedPosesMETA
    {
        public HandTrackingUnextrapolatedPosesMETA()
        {
            Type = METAHandTrackingUnextrapolatedPoses.TypeHandTrackingUnextrapolatedPosesMeta;
        }

        public readonly StructureType Type;

        public void* Next;

        public long CaptureTime;
    }
}
