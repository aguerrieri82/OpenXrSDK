using OpenXr.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Framework.Oculus
{

    public class XrMetaQuestTouchProHand<THand> : XrInteractionProfileHand<THand>
    {
        [XrPath("/input/thumbrest/force")]
        public XrInput ThumbrestForce;

        [XrPath("/input/stylus_fb/force")]
        public XrInput StylusForce;

        [XrPath("/input/trigger/curl_fb")]
        public XrInput TriggerCurl;

        [XrPath("/input/trigger/slide")]
        public XrInput TriggerSlide;

        [XrPath("/input/trigger/proximity_fb")]
        public XrInput TriggerProximity;

        [XrPath("/input/thumb_fb/proximity_fb")]
        public XrInput ThumbProximity;

        [XrPath("/output/trigger_haptic_fb")]
        public XrInput TriggerHaptic;

        [XrPath("/output/thumb_haptic_fb")]
        public XrInput ThumbHaptic;
    }

    [XrPath("/interaction_profiles/facebook/touch_controller_pro")]
    public class XrMetaQuestTouchPro 
    {
        [XrPath("/user/hand/left")]
        public XrMetaQuestTouchProHand<XrInteractionProfileHandLeft> Left;

        [XrPath("/user/hand/right")]
        public XrMetaQuestTouchProHand<XrInteractionProfileHandRight> Right;
    }
}
