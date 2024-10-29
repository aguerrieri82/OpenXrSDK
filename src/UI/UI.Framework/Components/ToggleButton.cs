namespace CanvasUI
{
    public class ToggleButton : UiContentView
    {
        public ToggleButton()
        {
            IsFocusable = true;
        }

        protected override void OnPointerUp(UiPointerEvent ev)
        {
            IsChecked = !IsChecked;
            base.OnPointerUp(ev);
        }

        protected void OnIsCheckedChanged(bool value, bool oldValue)
        {
            CheckedChange?.Invoke(this, EventArgs.Empty);
        }


        [UiProperty(false)]
        public bool IsChecked
        {
            get => GetValue<bool>(nameof(IsChecked))!;
            set => SetValue(nameof(IsChecked), value);
        }

        public event EventHandler? CheckedChange;
    }
}
