using Silk.NET.OpenXR;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;


namespace OpenXr.Framework.Oculus
{
    public enum BodyTrackingFidelityMETA
    {
        LowMeta = 1,
        HighMeta = 2
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct SystemPropertiesBodyTrackingFidelityMETA
    {
        public StructureType Type;
        public void* Next;
        public uint SupportsBodyTrackingFidelity;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct BodyTrackingFidelityStatusMETA
    {
        public StructureType Type;
        public void* Next;
        public BodyTrackingFidelityMETA Fidelity;
    }

    public class METABodyTrackingFidelity : BaseXrExtension
    {
        public const string ExtensionName = "XR_META_body_tracking_fidelity";

        public const StructureType TypeBodyTrackingFidelityStatusMeta = (StructureType)1000284000;
        public const StructureType TypeSystemPropertiesBodyTrackingFidelityMeta = (StructureType)1000284001;

        public METABodyTrackingFidelity(XR xr, Instance instance)
            : base(xr, instance)
        {
        }

        [AllowNull]
        public RequestBodyTrackingFidelityMETADelegate RequestBodyTrackingFidelityMETA;

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate Result RequestBodyTrackingFidelityMETADelegate(
            BodyTrackerFB bodyTracker,
            BodyTrackingFidelityMETA fidelity);
    }

    public class METABodyTrackingCalibration : BaseXrExtension
    {
        public const string ExtensionName = "XR_META_body_tracking_calibration";

        public METABodyTrackingCalibration(XR xr, Instance instance)
            : base(xr, instance)
        {
        }

        [AllowNull]
        public SuggestBodyTrackingCalibrationOverrideMETADelegate SuggestBodyTrackingCalibrationOverrideMETA;

        [AllowNull]
        public ResetBodyTrackingCalibrationMETADelegate ResetBodyTrackingCalibrationMETA;

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate Result SuggestBodyTrackingCalibrationOverrideMETADelegate(
            BodyTrackerFB bodyTracker,
            ref BodyTrackingCalibrationInfoMETA calibrationInfo);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate Result ResetBodyTrackingCalibrationMETADelegate(
            BodyTrackerFB bodyTracker);
    }
}