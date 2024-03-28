using System.Numerics;

namespace CanvasUI
{

    public delegate void UiEventHandler<T>(UiElement sender, T uiEvent) where T : UiEvent;

    public enum UiPointerButton
    {
        Left = 0x1,
        Middle = 0x2,
        Right = 0x4
    }

    public enum UiEventType
    {
        GotFocus,
        LostFocus,
        PointerDown,
        PointerUp,
        PointerMove,
        PointerEnter,
        PointerLeave
    }

    public enum UiEventDispatch
    {
        Direct,
        Bubble,
        Tunnel
    }

    public class UiEvent
    {
        public long Timestamp;

        public bool IsHandled;

        public UiEventType Type;
    }

    public class UiRoutedEvent : UiEvent
    {
        public UiElement? Source;

        public UiEventDispatch Dispatch;

        public bool StopBubble;
    }

    public class UiPointerEvent : UiRoutedEvent
    {
        public IUiPointer? Pointer;

        public Vector2 WindowPosition;

        public UiPointerButton Buttons;

        public int WheelDelta;
    }
}
