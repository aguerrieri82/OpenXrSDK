namespace XrEngine.Interaction
{
    public class RayEventSource : Behavior<Scene>, IRayTarget, IPointerEventSource
    {

        public RayEventSource()
        {
        }


        public void CapturePointer()
        {
            throw new NotImplementedException();
        }

        public void NotifyCollision(RenderContext ctx, Collision collision)
        {
            throw new NotImplementedException();
        }

        public void ReleasePointer()
        {
            throw new NotImplementedException();
        }

        public event PointerEventDelegate? PointerDown;

        public event PointerEventDelegate? PointerUp;

        public event PointerEventDelegate? PointerMove;

        public event PointerEventDelegate? WheelMove;

    }
}
