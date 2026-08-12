
using System.Diagnostics.CodeAnalysis;

namespace OpenXr.Framework
{
    public class XrInteractionProfileHand
    {
        [XrPath("/input/squeeze")]
        [AllowNull]
        public XrBoolInput SqueezeClick;

        [XrPath("/input/squeeze/value")]
        [AllowNull]
        public XrFloatInput SqueezeValue;

        [XrPath("/input/trigger")]
        [AllowNull]
        public XrBoolInput TriggerClick;

        [XrPath("/input/trigger/value")]
        [AllowNull]
        public XrFloatInput TriggerValue;

        [XrPath("/input/trigger/touch")]
        [AllowNull]
        public XrBoolInput TriggerTouch;

        [XrPath("/input/thumbstick")]
        [AllowNull]
        public XrVector2Input Thumbstick;

        [XrPath("/input/thumbstick/y")]
        [AllowNull]
        public XrFloatInput ThumbstickY;

        [XrPath("/input/thumbstick/x")]
        [AllowNull]
        public XrFloatInput ThumbstickX;

        [XrPath("/input/thumbstick/click")]
        [AllowNull]
        public XrBoolInput ThumbstickClick;

        [XrPath("/input/thumbstick/touch")]
        [AllowNull]
        public XrBoolInput ThumbstickTouch;

        [XrPath("/input/thumbrest/touch")]
        [AllowNull]
        public XrBoolInput ThumbrestTouch;

        [XrPath("/input/grip/pose")]
        [AllowNull]
        public XrPoseInput GripPose;

        [XrPath("/input/aim/pose")]
        [AllowNull]
        public XrPoseInput AimPose;

        [XrPath("/output/haptic")]
        [AllowNull]
        public XrHaptic Haptic;

        //XR_EXT_hand_interaction

        [XrPath("/input/pinch_ext/pose")]
        [AllowNull]
        public XrPoseInput PinchPose;

        [XrPath("/input/poke_ext/pose")]
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
        [AllowNull]
        public XrBoolInput XClick;

        [XrPath("/input/x/touch")]
        [AllowNull]
        public XrBoolInput XTouch;

        [XrPath("/input/y/click")]
        [AllowNull]
        public XrBoolInput YClick;

        [XrPath("/input/y/touch")]
        [AllowNull]
        public XrBoolInput YTouch;

        [XrPath("/input/menu/click")]
        [AllowNull]
        public XrBoolInput MenuClick;
    }

    public class XrInteractionProfileHandRight
    {
        [XrPath("/input/a/click")]
        [AllowNull]
        public XrBoolInput AClick;

        [XrPath("/input/a/touch")]
        [AllowNull]
        public XrBoolInput ATouch;

        [XrPath("/input/b/click")]
        [AllowNull]
        public XrBoolInput BClick;

        [XrPath("/input/b/touch")]
        [AllowNull]
        public XrBoolInput BTouch;
    }

}
