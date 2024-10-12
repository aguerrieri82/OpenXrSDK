namespace OpenXr.Framework
{
    public enum XrAnchorComponent
    {
        None = 0,
        Label = 0x1,
        Bounds = 0x2,
        Pose = 0x4,
        Mesh = 0x8,
        All = Label | Bounds | Pose | Mesh
    }

    public class XrAnchorFilter
    {
        public IList<Guid>? Ids { get; set; }

        public XrAnchorComponent Components { get; set; }

        public IList<string>? Labels { get; set; }
    }
}
