namespace XrInteraction
{

    public delegate void PointerEventDelegate(Pointer2Event ev);

    public interface IPointer2EventSource
    {
        void CapturePointer();

        void ReleasePointer();

        event PointerEventDelegate PointerDown;

        event PointerEventDelegate PointerUp;

        event PointerEventDelegate PointerMove;

        event PointerEventDelegate WheelMove;
    }
}
