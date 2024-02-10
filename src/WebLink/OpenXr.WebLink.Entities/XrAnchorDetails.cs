namespace OpenXr.WebLink.Entities
{
    public class XrAnchorDetails
    {
        public Guid Id { get; set; }

        public IList<string>? Labels { get; set; }

        public Rect2? Bounds2D { get; set; }

        public Pose? Pose { get; set; }

        public Mesh? Mesh { get; set; }
    }
}
