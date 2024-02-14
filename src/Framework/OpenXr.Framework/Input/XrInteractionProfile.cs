﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Framework.Input
{
    public class XrInteractionProfileHand<THand>
    {
        [XrPath("/input/squeeze")]
        public XrInput<bool>? SqueezeClick;

        [XrPath("/input/squeeze/value")]
        public XrInput<float>? Squeeze;

        [XrPath("/input/trigger")]
        public XrInput<bool>? TriggerClick;

        [XrPath("/input/trigger/value")]
        public XrInput<float>? TriggerValue;

        [XrPath("/input/trigger/touch")]
        public XrInput<bool>? TriggerTouch;

        [XrPath("/input/thumbstick")]
        public XrInput<Vector2>? Thumbstick;

        [XrPath("/input/thumbstick/y")]
        public XrInput<float>? ThumbstickY;

        [XrPath("/input/thumbstick/x")]
        public XrInput<float>? ThumbstickX;

        [XrPath("/input/thumbstick/click")]
        public XrInput<bool>? ThumbstickClick;

        [XrPath("/input/thumbstick/touch")]
        public XrInput<bool>? ThumbstickTouch;

        [XrPath("/input/thumbrest/touch")]
        public XrInput<bool>? ThumbrestTouch;

        [XrPath("/input/grip/pose")]
        public XrInput<XrPose>? GripPose;

        [XrPath("/input/aim/pose")]
        public XrInput<XrPose>? AimPose;

        public THand? Button;

    }

    public class XrInteractionProfileHandLeft
    {
        [XrPath("/input/x/click")]
        public XrInput<bool>? XClick;

        [XrPath("/input/x/touch")]
        public XrInput<bool>? XTouch;

        [XrPath("/input/y/click")]
        public XrInput<bool>? YClick;

        [XrPath("/input/y/touch")]
        public XrInput<bool>? YTouch;

        [XrPath("/input/menu/click")]
        public XrInput<bool>? MenuClick;
    }

    public class XrInteractionProfileHandRight
    {
        [XrPath("/input/a/click")]
        public XrInput<bool>? AClick;

        [XrPath("/input/a/touch")]
        public XrInput<bool>? ATouch;

        [XrPath("/input/b/click")]
        public XrInput<bool>? BClick;

        [XrPath("/input/b/touch")]
        public XrInput<bool>? BTouch;
    }

}