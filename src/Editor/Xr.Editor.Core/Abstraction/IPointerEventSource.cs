namespace Xr.Editor
{
    public struct PointerEvent
    {
        public float X;

        public float Y;

        public MouseButton Buttons;

        public int WheelDelta;

        public readonly bool IsLeftDown => (Buttons & MouseButton.Left) == MouseButton.Left;

        public readonly bool IsMiddleDown => (Buttons & MouseButton.Middle) == MouseButton.Middle;

        public readonly bool IsRightDown => (Buttons & MouseButton.Right) == MouseButton.Right;


    }

    public delegate void PointerEventDelegate(PointerEvent ev);

    public interface IPointerEventSource
    {
        void CapturePointer();

        void ReleasePointer();


        event PointerEventDelegate PointerDown;

        event PointerEventDelegate PointerUp;

        event PointerEventDelegate PointerMove;


        event PointerEventDelegate WheelMove;
    }
}
