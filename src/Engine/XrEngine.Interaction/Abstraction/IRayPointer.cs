using XrMath;

namespace XrEngine.Interaction
{
    public struct RayPointerStatus
    {
        public Ray3 Ray;

        public PointerButton Buttons;

        public bool IsActive;
    }

    public interface IRayPointer
    {
        RayPointerStatus GetPointerStatus();
    }
}
