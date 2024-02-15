namespace OpenXr.Framework
{
    public class XrAnchor
    {
        public Guid Id { get; set; }

        public IList<string>? Labels { get; set; }

        public Rect2? Bounds2D { get; set; }

        public XrPose? Pose { get; set; }

        public XrMesh? Mesh { get; set; }
    }
}
