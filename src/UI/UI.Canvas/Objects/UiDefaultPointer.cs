using System.Reflection;

namespace CanvasUI
{
    public readonly struct UiDefaultPointer : IUiPointer
    {
        public UiDefaultPointer(int id)
        {
            Id = id;
        }

        public readonly void Capture(UiElement element)
        {
            UiManager.SetPointerCapture(Id, element);
        }

        public readonly void Release()
        {
            UiManager.SetPointerCapture(Id, null);
        }

        public UiPointerButton Buttons => UiPointerButton.None;

        public int Id { get; }

    }
}
