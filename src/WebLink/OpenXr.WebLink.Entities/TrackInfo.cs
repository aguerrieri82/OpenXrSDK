﻿using OpenXr.Framework;

namespace OpenXr.WebLink.Entities
{
    public enum TrackObjectType
    {
        Head,
        ControllerL,
        ControllerR,
        Anchor
    }

    public class TrackInfo
    {
        public TrackObjectType ObjectType { get; set; }

        public Guid? AnchorId { get; set; }

        public XrPose? Pose { get; set; }
    }
}
