using Silk.NET.OpenXR;

namespace OpenXr.Framework.Oculus
{
    public static class METASimultaneousHandsAndControllers
    {
        public const string ExtensionName = "XR_META_simultaneous_hands_and_controllers";

    }

    public delegate Result ResumeSimultaneousHandsAndControllersTrackingMETADelegate(
        Session session,
        ref SimultaneousHandsAndControllersTrackingResumeInfoMETA resumeInfo);

    public delegate Result PauseSimultaneousHandsAndControllersTrackingMETADelegate(
        Session session,
        ref SimultaneousHandsAndControllersTrackingPauseInfoMETA pauseInfo);
}
