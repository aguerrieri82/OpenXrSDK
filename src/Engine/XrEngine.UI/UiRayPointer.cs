using CanvasUI;
using XrEngine.Interaction;

namespace XrEngine.UI
{
    public readonly struct UiRayPointer : IUiPointer
    {
        readonly IRayPointer _pointer;

        public UiRayPointer(IRayPointer rayPointer)
        {
            _pointer = rayPointer;
        }

        public readonly void Capture(UiElement element)
        {
            UiManager.SetPointerCapture(Id, element);
            _pointer.CapturePointer();
        }

        public readonly void Release()
        {
            UiManager.SetPointerCapture(Id, null);
            _pointer.ReleasePointer();
        }

        public readonly int Id => _pointer.PointerId;
    }
}
