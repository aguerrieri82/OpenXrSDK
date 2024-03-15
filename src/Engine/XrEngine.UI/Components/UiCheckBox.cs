using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine.UI.Components
{
    public class UiCheckBox : UiContainer
    {
        protected readonly UiToggleButton _toggle;
        protected readonly UiContentView _content;
        protected readonly UiIcon _checkedContent;
        protected readonly UiIcon _uncheckedContent;

        public UiCheckBox()
        {
            _toggle = this.AddChild<UiToggleButton>();
            _toggle.CheckedChange += OnCheckedChange;

            _checkedContent = new UiIcon() { Icon = IconName.IconCheckBox };
            _uncheckedContent = new UiIcon() { Icon = IconName.IconCheckBoxOutlineBlank };

            _content = this.AddChild<UiContentView>();

            Style.Layout = UiLayoutType.Flex;
            Style.FlexDirection = UIOrientation.Horizontal;
            Style.ColGap = UnitValue.Dp(8);
            Style.AlignItems = UiAlignment.Center;
        }

        private void OnCheckedChange(object? sender, EventArgs e)
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
        
        public bool IsChecked
        {
            get => _toggle.IsChecked;
            set => _toggle.IsChecked = value;
        }

        public UiToggleButton Toggle => _toggle;
    }
}
