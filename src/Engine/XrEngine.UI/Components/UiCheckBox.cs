using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine.UI.Components
{
    public class UiCheckBox : UiContainer, IInputElement<bool>
    {
        protected readonly UiToggleButton _toggle;
        protected readonly UiContentView _content;
        protected readonly UiIcon _checkedContent;
        protected readonly UiIcon _uncheckedContent;

        public UiCheckBox()
        {
            IsFocusable = true;

            _toggle = this.AddChild<UiToggleButton>();
            _toggle.CheckedChange += OnValueChange;
            _toggle.Style.FontSize = UnitValue.Dp(24);

            _checkedContent = new UiIcon() { Icon = IconName.IconCheckBox };
            _uncheckedContent = new UiIcon() { Icon = IconName.IconCheckBoxOutlineBlank };

            _content = this.AddChild<UiContentView>();
            _content.PointerUp += OnContentPointerUp;

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

        public UiToggleButton Toggle => _toggle;
    }
}
