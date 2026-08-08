using Silk.NET.OpenXR;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace OpenXr.Framework.Oculus
{
    public static class MetaHandTrackingFrequencyHint
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
