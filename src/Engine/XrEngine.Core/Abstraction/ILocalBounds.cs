using XrMath;

namespace XrEngine
{
    public enum UpdateMode
    {
        Manual,
        Automatic
    }

    public interface ILocalBounds
    {
        void UpdateBounds(bool force = false);

        Bounds3 LocalBounds { get; }

        UpdateMode BoundUpdateMode { get; set; }
    }
}
