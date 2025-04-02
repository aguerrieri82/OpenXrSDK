namespace XrEngine
{
    public enum TessellationMode
    {
        None,
        Normal,
        Geometry
    }

    public interface ITessellationMaterial : IMaterial
    {
        TessellationMode TessellationMode { get; }

        bool DebugTessellation { get; }
    }
}
