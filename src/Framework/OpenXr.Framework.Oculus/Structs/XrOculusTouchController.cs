
using System.Diagnostics.CodeAnalysis;

namespace OpenXr.Framework.Oculus
{

    public class XrOculusTouchControllerHand<THand> : XrInteractionProfileHand<THand>
    {
        [XrPath("/input/thumbrest/force")]
        [AllowNull]
        public XrInput<float> ThumbrestForce;

        [XrPath("/input/stylus_fb/force")]
        [AllowNull]
        public XrInput<float> StylusForce;

        [XrPath("/input/trigger/curl_fb")]
        [AllowNull]
        public XrInput<float> TriggerCurl;

        [XrPath("/input/trigger/slide")]
        [AllowNull]
        public XrInput<float> TriggerSlide;

        [XrPath("/input/trigger/proximity_fb")]
        [AllowNull]
        public XrInput<bool> TriggerProximity;

        [XrPath("/input/thumb_fb/proximity_fb")]
        [AllowNull]
        public XrInput<bool> ThumbProximity;

        [XrPath("/output/trigger_haptic_fb")]
        [AllowNull]
        public XrHaptic TriggerHaptic;

        [XrPath("/output/thumb_haptic_fb")]
        [AllowNull]
        public XrHaptic ThumbHaptic;

        //XR_META_hand_tracking_microgestures

        [XrPath("/input/swipe_left_meta/click")]
        [AllowNull]
        public XrBoolInput SwipeLeft;


        [XrPath("/input/swipe_right_meta/click")]
        [AllowNull]
        public XrBoolInput SwipeRight;

        [XrPath("/input/swipe_forward_meta/click")]
        [AllowNull]
        public XrBoolInput SwipeForward;

        [XrPath("/input/swipe_backward_meta/click")]
        [AllowNull]
        public XrBoolInput SwipeBackward;

        [XrPath("/input/tap_thumb_meta/click")]
        [AllowNull]
        public XrBoolInput TapThumb;

    }


    [XrPath("/interaction_profiles/meta/touch_controller_plus")]
    [XrPath("/interaction_profiles/oculus/touch_controller")]
    [XrPath("/interaction_profiles/oculus/touch_controller_pro")]
    [XrPath("/interaction_profiles/facebook/touch_controller_pro")]
    [XrPath("/interaction_profiles/khr/simple_controller")]
    [XrPath("/interaction_profiles/ext/hand_interaction_ext")]
    public class XrOculusTouchController : IXrBasicInteractionProfile
    {
        [XrPath("/user/hand/left")]
        [AllowNull]
        public XrOculusTouchControllerHand<XrInteractionProfileHandLeft> Left;

        [XrPath("/user/hand/right")]
        [AllowNull]
        public XrOculusTouchControllerHand<XrInteractionProfileHandRight> Right;

        XrInteractionProfileHand<XrInteractionProfileHandLeft> IXrBasicInteractionProfile.Left => Left;

        XrInteractionProfileHand<XrInteractionProfileHandRight> IXrBasicInteractionProfile.Right => Right;
    }
}
