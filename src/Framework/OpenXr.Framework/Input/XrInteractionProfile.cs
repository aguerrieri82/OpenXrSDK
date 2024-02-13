using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Framework.Input
{
    public class XrInteractionProfileHand<THand>
    {
        [XrPath("/input/squeeze/value")]
        public XrInput Squeeze;

        [XrPath("/input/trigger/value")]
        public XrInput TriggerValue;

        [XrPath("/input/trigger/touch")]
        public XrInput TriggerTouch;

        [XrPath("/input/thumbstick/y")]
        public XrInput ThumbstickY;

        [XrPath("/input/thumbstick/x")]
        public XrInput ThumbstickX;

        [XrPath("/input/thumbstick/click")]
        public XrInput ThumbstickClick;

        [XrPath("/input/thumbstick/touch")]
        public XrInput ThumbstickTouch;

        [XrPath("/input/thumbrest/touch")]
        public XrInput ThumbrestTouch;


        [XrPath("/input/grip/pose")]
        public XrInput GripPose;


        [XrPath("/input/aim/pose")]
        public XrInput GripAim;

        public THand Buttons;

    }

    public class XrInteractionProfileHandLeft
    {
        [XrPath("/input/x/click")]
        public XrInput XClick;

        [XrPath("/input/x/touch")]
        public XrInput XTouch;

        [XrPath("/input/y/click")]
        public XrInput YClick;

        [XrPath("/input/y/touch")]
        public XrInput YTouch;

        [XrPath("/input/menu/click")]
        public XrInput MenuClick;
    }

    public class XrInteractionProfileHandRight
    {
        [XrPath("/input/a/click")]
        public XrInput AClick;

        [XrPath("/input/a/touch")]
        public XrInput ATouch;

        [XrPath("/input/b/click")]
        public XrInput BClick;

        [XrPath("/input/b/touch")]
        public XrInput BTouch;
    }

}
