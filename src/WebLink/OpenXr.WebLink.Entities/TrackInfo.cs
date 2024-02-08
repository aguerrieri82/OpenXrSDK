using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public Posef Pose { get; set; }
    }
}
