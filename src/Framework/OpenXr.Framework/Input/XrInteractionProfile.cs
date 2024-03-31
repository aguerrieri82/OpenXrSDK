using System.Numerics;
using XrMath;

namespace OpenXr.Framework
{
    public class XrInteractionProfileHand<THand>
    {
        [XrPath("/input/squeeze")]
        public XrBoolInput? SqueezeClick;

        [XrPath("/input/squeeze/value")]
        public XrFloatInput? SqueezeValue;

        [XrPath("/input/trigger")]
        public XrBoolInput? TriggerClick;

        [XrPath("/input/trigger/value")]
        public XrFloatInput? TriggerValue;

        [XrPath("/input/trigger/touch")]
        public XrBoolInput? TriggerTouch;

        [XrPath("/input/thumbstick")]
        public XrVector2Input? Thumbstick;

        [XrPath("/input/thumbstick/y")]
        public XrFloatInput? ThumbstickY;

        [XrPath("/input/thumbstick/x")]
        public XrFloatInput? ThumbstickX;

        [XrPath("/input/thumbstick/click")]
        public XrBoolInput? ThumbstickClick;

        [XrPath("/input/thumbstick/touch")]
        public XrBoolInput? ThumbstickTouch;

        [XrPath("/input/thumbrest/touch")]
        public XrBoolInput? ThumbrestTouch;

        [XrPath("/input/grip/pose")]
        public XrPoseInput? GripPose;

        [XrPath("/input/aim/pose")]
        public XrPoseInput? AimPose;

        [XrPath("/output/haptic")]
        public XrHaptic? Haptic;

        public THand? Button;

    }

    public class XrInteractionProfileHandLeft
    {
        [XrPath("/input/x/click")]
        public XrBoolInput? XClick;

        [XrPath("/input/x/touch")]
        public XrBoolInput? XTouch;

        [XrPath("/input/y/click")]
        public XrBoolInput? YClick;

        [XrPath("/input/y/touch")]
        public XrBoolInput? YTouch;

        [XrPath("/input/menu/click")]
        public XrBoolInput? MenuClick;
    }

    public class XrInteractionProfileHandRight
    {
        [XrPath("/input/a/click")]
        public XrBoolInput? AClick;

        [XrPath("/input/a/touch")]
        public XrBoolInput? ATouch;

        [XrPath("/input/b/click")]
        public XrBoolInput? BClick;

        [XrPath("/input/b/touch")]
        public XrBoolInput? BTouch;
    }

}
