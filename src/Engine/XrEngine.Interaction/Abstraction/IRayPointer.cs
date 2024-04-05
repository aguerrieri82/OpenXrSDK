using XrMath;

namespace XrEngine.Interaction
{
    public struct RayPointerStatus
    {
        public Ray3 Ray;

        public Pointer2Button Buttons;

        public bool IsActive;
    }

    public interface IRayPointer
    {
        RayPointerStatus GetPointerStatus();

        void CapturePointer();

        void ReleasePointer();

        int PointerId { get; }
    }
}
