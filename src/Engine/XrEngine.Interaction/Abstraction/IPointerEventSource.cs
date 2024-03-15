namespace XrEngine.Interaction
{

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
