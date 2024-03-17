

namespace CanvasUI
{
    public class UiButton : UiContentView
    {
        public UiButton()
        {
            IsFocusable = true;
        }

        protected override void OnPointerUp(UiPointerEvent ev)
        {
            Click?.Invoke(this, EventArgs.Empty);
            base.OnPointerUp(ev);
        }


        public event EventHandler? Click;
    }
}
