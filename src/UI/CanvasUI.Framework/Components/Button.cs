

namespace CanvasUI
{
    public class Button : UiContentView
    {
        public Button()
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
