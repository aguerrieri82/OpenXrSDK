using Silk.NET.OpenXR;
using System.Numerics;
using XrMath;

namespace OpenXr.Framework
{
    public class XrSpaceLocation
    {

        public Pose3 Pose;

        public Vector3 LinearVelocity;

        public Vector3 AngularVelocity;

        public SpaceLocationFlags Flags;

        public SpaceVelocityFlags VelocityFlags;

        public bool IsValid => (Flags & SpaceLocationFlags.OrientationValidBit) != 0 &&
                       (Flags & SpaceLocationFlags.PositionValidBit) != 0;


    }
}
