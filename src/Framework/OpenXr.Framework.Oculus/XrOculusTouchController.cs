namespace OpenXr.Framework.Oculus
{

    public class XrOculusTouchControllerHand<THand> : XrInteractionProfileHand<THand>
    {
        [XrPath("/input/thumbrest/force")]
        public XrInput<float>? ThumbrestForce;

        [XrPath("/input/stylus_fb/force")]
        public XrInput<float>? StylusForce;

        [XrPath("/input/trigger/curl_fb")]
        public XrInput<float>? TriggerCurl;

        [XrPath("/input/trigger/slide")]
        public XrInput<float>? TriggerSlide;

        [XrPath("/input/trigger/proximity_fb")]
        public XrInput<bool>? TriggerProximity;

        [XrPath("/input/thumb_fb/proximity_fb")]
        public XrInput<bool>? ThumbProximity;

        [XrPath("/output/trigger_haptic_fb")]
        public XrHaptic? TriggerHaptic;

        [XrPath("/output/thumb_haptic_fb")]
        public XrHaptic? ThumbHaptic;
    }

    //[XrPath("/interaction_profiles/facebook/touch_controller_pro")]
    [XrPath("/interaction_profiles/oculus/touch_controller")]
    public class XrOculusTouchController
    {
        [XrPath("/user/hand/left")]
        public XrOculusTouchControllerHand<XrInteractionProfileHandLeft>? Left;

        [XrPath("/user/hand/right")]
        public XrOculusTouchControllerHand<XrInteractionProfileHandRight>? Right;
    }
}
