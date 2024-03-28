namespace CanvasUI
{
    public class CheckBox : UiContainer, IInputElement<bool>
    {
        protected readonly ToggleButton _toggle;
        protected readonly UiContentView _content;
        protected readonly Icon _checkedContent;
        protected readonly Icon _uncheckedContent;

        public CheckBox()
        {
            IsFocusable = true;

            _toggle = this.AddChild<ToggleButton>();
            _toggle.CheckedChange += OnValueChange;
            _toggle.Style.FontSize = UnitValue.Dp(24);
            _toggle.Style.Padding = UnitRectValue.All(-3f / 24f, Unit.Em);


            _checkedContent = new Icon() { IconName = IconName.IconCheckBox };
            _uncheckedContent = new Icon() { IconName = IconName.IconCheckBoxOutlineBlank };

            _content = this.AddChild<UiContentView>();
            _content.PointerUp += OnContentPointerUp;
            _content.Style.Margin = UnitRectValue.Set(4);

            Style.Layout = UiLayoutType.Flex;
            Style.FlexDirection = UIOrientation.Horizontal;
            Style.ColGap = UnitValue.Dp(4);
            Style.AlignItems = UiAlignment.Center;

            _toggle.Content = _uncheckedContent;
        }

        private void OnContentPointerUp(UiElement sender, UiPointerEvent uiEvent)
        {
            _toggle.IsChecked = !_toggle.IsChecked;
        }

        private void OnValueChange(object? sender, EventArgs e)
        {
            if (_toggle.IsChecked)
                _toggle.Content = _checkedContent;
            else
                _toggle.Content = _uncheckedContent;

            ValueChanged?.Invoke(this, _toggle.IsChecked, !_toggle.IsChecked);
        }


        public object? Content
        {
            get => _content.Content;
            set => _content.Content = value;
        }

        public bool Value
        {
            get => _toggle.IsChecked;
            set => _toggle.IsChecked = value;
        }

        public ToggleButton Toggle => _toggle;

        public event InputValueChangedHandler<bool> ValueChanged;
    }
}
