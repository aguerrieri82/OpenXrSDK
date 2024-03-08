using Silk.NET.OpenXR;
using XrMath;

namespace OpenXr.Framework
{
    public class XrSpaceLocation
    {

        public Pose3 Pose;

        public SpaceLocationFlags Flags;

        public bool IsValid => (Flags & SpaceLocationFlags.OrientationValidBit) != 0 &&
                       (Flags & SpaceLocationFlags.PositionValidBit) != 0;
    }
}
