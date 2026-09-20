using CanvasUI;
using XrInteraction;

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

            if (_pointer is IRayPointer ray)
                ray.CapturePointer();
        }

        public readonly void Release()
        {
            UiManager.SetPointerCapture(Id, null);

            if (_pointer is IRayPointer ray)
                ray.ReleasePointer();
        }

        public UiPointerButton Buttons => (UiPointerButton)_pointer.GetPointerStatus().Buttons;

        public readonly int Id => _pointer.PointerId;
    }


    public readonly struct UiTouchPointer : IUiPointer
    {
        readonly ITouchPointer _pointer;
        readonly bool _isDown;

        public UiTouchPointer(ITouchPointer rayPointer, bool isDown)
        {
            _pointer = rayPointer;
            _isDown = isDown;
        }

        public readonly void Capture(UiElement element)
        {
            UiManager.SetPointerCapture(Id, element);
        }

        public readonly void Release()
        {
            UiManager.SetPointerCapture(Id, null);
        }

        public UiPointerButton Buttons => _isDown ? UiPointerButton.Left : UiPointerButton.None;

        public readonly int Id => _pointer.PointerId;
    }
}
