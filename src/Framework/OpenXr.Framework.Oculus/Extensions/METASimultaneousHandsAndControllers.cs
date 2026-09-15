using Silk.NET.OpenXR;
using System.Diagnostics.CodeAnalysis;

namespace OpenXr.Framework.Oculus
{
    public class METASimultaneousHandsAndControllers : BaseXrExtension
    {

        public METASimultaneousHandsAndControllers(XR xr, Instance instance)
            : base(xr, instance)
        {

        }

        [AllowNull]
        public PauseSimultaneousHandsAndControllersTrackingMETADelegate PauseSimultaneousHandsAndControllersTrackingMETA;

        [AllowNull]
        public ResumeSimultaneousHandsAndControllersTrackingMETADelegate ResumeSimultaneousHandsAndControllersTrackingMETA;


        public delegate Result ResumeSimultaneousHandsAndControllersTrackingMETADelegate(
            Session session,
            ref SimultaneousHandsAndControllersTrackingResumeInfoMETA resumeInfo);

        public delegate Result PauseSimultaneousHandsAndControllersTrackingMETADelegate(
            Session session,
            ref SimultaneousHandsAndControllersTrackingPauseInfoMETA pauseInfo);

        public const string ExtensionName = "XR_META_simultaneous_hands_and_controllers";

    }

}
