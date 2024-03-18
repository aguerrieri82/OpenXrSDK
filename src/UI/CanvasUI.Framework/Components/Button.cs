
using XrMath;

namespace CanvasUI
{
    public class Button : UiContentView
    {
        StyleValue<Color?> _oldBk;

        public Button()
        {
            IsFocusable = true;
        }

        protected override void OnPointerDown(UiPointerEvent ev)
        {
            _oldBk = Style.BackgroundColor;
            Style.BackgroundColor = new Color(1, 1, 1);
            base.OnPointerDown(ev);
        }

        protected override void OnPointerUp(UiPointerEvent ev)
        {
            Click?.Invoke(this, EventArgs.Empty);
            Style.BackgroundColor = _oldBk;
            base.OnPointerUp(ev);
        }


        public event EventHandler? Click;
    }
}
