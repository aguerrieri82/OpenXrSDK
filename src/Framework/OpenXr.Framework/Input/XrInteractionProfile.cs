using OpenXr.Framework.Input;
using System.Diagnostics.CodeAnalysis;

namespace OpenXr.Framework
{
    public class XrInteractionProfileHand
    {
        [XrPath("/input/squeeze")]
        [XrProfile(XrProfiles.TouchPlus, XrProfiles.Touch, XrProfiles.TouchPro)]
        [AllowNull]
        public XrBoolInput SqueezeClick;

        [XrPath("/input/squeeze/value")]
        [XrProfile(XrProfiles.TouchPlus, XrProfiles.Touch, XrProfiles.TouchPro)]
        [AllowNull]
        public XrFloatInput SqueezeValue;

        [XrPath("/input/trigger")]
        [XrProfile(XrProfiles.TouchPlus, XrProfiles.Touch, XrProfiles.TouchPro)]
        [AllowNull]
        public XrBoolInput TriggerClick;

        [XrPath("/input/trigger/value")]
        [XrProfile(XrProfiles.TouchPlus, XrProfiles.Touch, XrProfiles.TouchPro)]
        [AllowNull]
        public XrFloatInput TriggerValue;

        [XrPath("/input/trigger/touch")]
        [XrProfile(XrProfiles.TouchPlus, XrProfiles.Touch, XrProfiles.TouchPro)]
        [AllowNull]
        public XrBoolInput TriggerTouch;

        [XrPath("/input/thumbstick")]
        [XrProfile(XrProfiles.TouchPlus, XrProfiles.Touch, XrProfiles.TouchPro)]
        [AllowNull]
        public XrVector2Input Thumbstick;

        [XrPath("/input/thumbstick/y")]
        [XrProfile(XrProfiles.TouchPlus, XrProfiles.Touch, XrProfiles.TouchPro)]
        [AllowNull]
        public XrFloatInput ThumbstickY;

        [XrPath("/input/thumbstick/x")]
        [XrProfile(XrProfiles.TouchPlus, XrProfiles.Touch, XrProfiles.TouchPro)]
        [AllowNull]
        public XrFloatInput ThumbstickX;

        [XrPath("/input/thumbstick/click")]
        [XrProfile(XrProfiles.TouchPlus, XrProfiles.Touch, XrProfiles.TouchPro)]
        [AllowNull]
        public XrBoolInput ThumbstickClick;

        [XrPath("/input/thumbstick/touch")]
        [XrProfile(XrProfiles.TouchPlus, XrProfiles.Touch, XrProfiles.TouchPro)]
        [AllowNull]
        public XrBoolInput ThumbstickTouch;

        [XrPath("/input/thumbrest/touch")]
        [XrProfile(XrProfiles.TouchPlus, XrProfiles.Touch, XrProfiles.TouchPro)]
        [AllowNull]
        public XrBoolInput ThumbrestTouch;

        [XrPath("/input/grip/pose")]
        [XrProfile(XrProfiles.TouchPlus, XrProfiles.Touch, XrProfiles.TouchPro, XrProfiles.Simple, XrProfiles.Hand)]
        [AllowNull]
        public XrPoseInput GripPose;

        [XrPath("/input/aim/pose")]
        [XrProfile(XrProfiles.TouchPlus, XrProfiles.Touch, XrProfiles.TouchPro, XrProfiles.Simple, XrProfiles.Hand)]
        [AllowNull]
        public XrPoseInput AimPose;

        [XrPath("/output/haptic")]
        [XrProfile(XrProfiles.TouchPlus, XrProfiles.Touch, XrProfiles.TouchPro, XrProfiles.Simple)]
        [AllowNull]
        public XrHaptic Haptic;

        [XrPath("/input/pinch_ext/pose")]
        [XrProfile(XrProfiles.Hand)]
        [AllowNull]
        public XrPoseInput PinchPose;

        [XrPath("/input/poke_ext/pose")]
        [XrProfile(XrProfiles.Hand)]
        [AllowNull]
        public XrPoseInput PokePose;
    }

    public class XrInteractionProfileHand<THand> : XrInteractionProfileHand
    {
        [AllowNull]
        public THand Button;
    }

    public class XrInteractionProfileHandLeft
    {
        [XrPath("/input/x/click")]
        [XrProfile(XrProfiles.TouchPlus, XrProfiles.Touch, XrProfiles.TouchPro)]
        [AllowNull]
        public XrBoolInput XClick;

        [XrPath("/input/x/touch")]
        [XrProfile(XrProfiles.TouchPlus, XrProfiles.Touch, XrProfiles.TouchPro)]
        [AllowNull]
        public XrBoolInput XTouch;

        [XrPath("/input/y/click")]
        [XrProfile(XrProfiles.TouchPlus, XrProfiles.Touch, XrProfiles.TouchPro)]
        [AllowNull]
        public XrBoolInput YClick;

        [XrPath("/input/y/touch")]
        [XrProfile(XrProfiles.TouchPlus, XrProfiles.Touch, XrProfiles.TouchPro)]
        [AllowNull]
        public XrBoolInput YTouch;

        [XrPath("/input/menu/click")]
        [XrProfile(XrProfiles.TouchPlus, XrProfiles.Touch, XrProfiles.TouchPro, XrProfiles.Simple)]
        [AllowNull]
        public XrBoolInput MenuClick;
    }

    public class XrInteractionProfileHandRight
    {
        [XrPath("/input/a/click")]
        [XrProfile(XrProfiles.TouchPlus, XrProfiles.Touch, XrProfiles.TouchPro)]
        [AllowNull]
        public XrBoolInput AClick;

        [XrPath("/input/a/touch")]
        [XrProfile(XrProfiles.TouchPlus, XrProfiles.Touch, XrProfiles.TouchPro)]
        [AllowNull]
        public XrBoolInput ATouch;

        [XrPath("/input/b/click")]
        [XrProfile(XrProfiles.TouchPlus, XrProfiles.Touch, XrProfiles.TouchPro)]
        [AllowNull]
        public XrBoolInput BClick;

        [XrPath("/input/b/touch")]
        [XrProfile(XrProfiles.TouchPlus, XrProfiles.Touch, XrProfiles.TouchPro)]
        [AllowNull]
        public XrBoolInput BTouch;
    }
}