using Silk.NET.OpenXR;
using Xr.Math;

namespace OpenXr.Framework
{
    public class XrSpaceLocation
    {
        public bool IsValid => (Flags & SpaceLocationFlags.OrientationValidBit) != 0 &&
                               (Flags & SpaceLocationFlags.PositionValidBit) != 0;

        public Pose3 Pose;

        public SpaceLocationFlags Flags;
    }
}
