using XrMath;

namespace XrInteraction
{
    public struct RayPointerStatus
    {
        public Ray3 Ray;

        public Pointer2Button Buttons;

        public bool IsActive;
    }

    public interface IRayPointer : IPointer
    {
        RayPointerStatus GetPointerStatus();

        void CapturePointer();

        void ReleasePointer();

        bool IsCaptured { get; }
    }
}
