using Silk.NET.OpenXR;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace OpenXr.Framework.Oculus
{
    public class METAHandTrackingFrequencyHint : BaseXrExtension
    {
        public const string ExtensionName = "XR_META_hand_tracking_frequency_hint";

        public METAHandTrackingFrequencyHint(XR xr, Instance instance) : base(xr, instance)
        {
        }

        [AllowNull]
        public SetHandTrackingFrequencyHintMETADelegate SetHandTrackingFrequencyHintMETA;

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate Result SetHandTrackingFrequencyHintMETADelegate(Session session, HandTrackingFrequencyHintMETA frequencyHint);
    }

    public enum HandTrackingFrequencyHintMETA : int
    {
        DefaultMeta = 1,
        HighMeta = 2,
        MaxEnumMeta = 0x7FFFFFFF
    }
}