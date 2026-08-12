using OpenXr.Framework.Input;
using System.Diagnostics.CodeAnalysis;

namespace OpenXr.Framework.Oculus
{
    public class XrOculusTouchControllerHand<THand> : XrInteractionProfileHand<THand>
    {
        [XrPath("/input/thumbrest/force")]
        [XrProfile(XrProfiles.TouchPro)]
        [AllowNull]
        public XrInput<float> ThumbrestForce;

        [XrPath("/input/stylus/force")]
        [XrProfile(XrProfiles.TouchPro)]
        [AllowNull]
        public XrInput<float> StylusForce;

        [XrPath("/input/trigger_curl/value")]
        [XrProfile(XrProfiles.TouchPlus, XrProfiles.TouchPro)]
        [AllowNull]
        public XrInput<float> TriggerCurl;

        [XrPath("/input/trigger_slide/value")]
        [XrProfile(XrProfiles.TouchPlus, XrProfiles.TouchPro)]
        [AllowNull]
        public XrInput<float> TriggerSlide;

        [XrPath("/input/trigger/proximity")]
        [XrProfile(XrProfiles.Touch, XrProfiles.TouchPlus, XrProfiles.TouchPro)]
        [AllowNull]
        public XrInput<bool> TriggerProximity;

        [XrPath("/input/thumb_resting_surfaces/proximity")]
        [XrProfile(XrProfiles.Touch, XrProfiles.TouchPlus, XrProfiles.TouchPro)]
        [AllowNull]
        public XrInput<bool> ThumbProximity;

        [XrPath("/output/haptic_trigger")]
        [XrProfile(XrProfiles.TouchPro)]
        [AllowNull]
        public XrHaptic TriggerHaptic;

        [XrPath("/output/haptic_thumb")]
        [XrProfile(XrProfiles.TouchPro)]
        [AllowNull]
        public XrHaptic ThumbHaptic;

        // XR_META_hand_tracking_microgestures

        [XrPath("/input/swipe_left_meta/click")]
        [XrProfile(XrProfiles.Hand)]
        [AllowNull]
        public XrBoolInput SwipeLeft;

        [XrPath("/input/swipe_right_meta/click")]
        [XrProfile(XrProfiles.Hand)]
        [AllowNull]
        public XrBoolInput SwipeRight;

        [XrPath("/input/swipe_forward_meta/click")]
        [XrProfile(XrProfiles.Hand)]
        [AllowNull]
        public XrBoolInput SwipeForward;

        [XrPath("/input/swipe_backward_meta/click")]
        [XrProfile(XrProfiles.Hand)]
        [AllowNull]
        public XrBoolInput SwipeBackward;

        [XrPath("/input/tap_thumb_meta/click")]
        [XrProfile(XrProfiles.Hand)]
        [AllowNull]
        public XrBoolInput TapThumb;
    }

    public class XrOculusTouchController : IXrBasicInteractionProfile
    {
        [XrPath("/user/hand/left")]
        [AllowNull]
        public XrOculusTouchControllerHand<XrInteractionProfileHandLeft> Left;

        [XrPath("/user/hand/right")]
        [AllowNull]
        public XrOculusTouchControllerHand<XrInteractionProfileHandRight> Right;

        [XrPath("/user/detached_controller_meta/left")]
        [AllowNull]
        public XrInteractionProfileHand DetachedLeft;

        [XrPath("/user/detached_controller_meta/right")]
        [AllowNull]
        public XrInteractionProfileHand DetachedRight;

        XrInteractionProfileHand<XrInteractionProfileHandLeft> IXrBasicInteractionProfile.Left => Left;
        XrInteractionProfileHand<XrInteractionProfileHandRight> IXrBasicInteractionProfile.Right => Right;
    }
}