using Silk.NET.OpenXR;
using System.Runtime.InteropServices;

namespace OpenXr.Framework.Oculus
{
    public static class METAHandTrackingFrequencyHint
    {
        public const string ExtensionName = "XR_META_hand_tracking_frequency_hint";

    }

    public enum HandTrackingFrequencyHintMETA : int
    {
        DefaultMeta = 1,
        HighMeta = 2,
        MaxEnumMeta = 0x7FFFFFFF
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate Result SetHandTrackingFrequencyHintMETADelegate(Session session, HandTrackingFrequencyHintMETA frequencyHint);
}
