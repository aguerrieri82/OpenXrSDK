using Silk.NET.OpenXR;

namespace OpenXr.Framework
{
    public class XrSpaceLocation
    {
        public XrPose? Pose { get; set; }

        public SpaceLocationFlags Flags { get; set; }
    }
}
