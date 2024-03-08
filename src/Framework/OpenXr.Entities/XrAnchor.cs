using Xr.Math;

namespace OpenXr.Framework
{
    public class XrAnchor
    {
        public Guid Id { get; set; }

        public IList<string>? Labels { get; set; }

        public Rect2? Bounds2D { get; set; }

        public Pose3? Pose { get; set; }

        public Mesh? Mesh { get; set; }
    }
}
