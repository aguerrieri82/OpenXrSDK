using System.Windows;
using System.Windows.Controls;

namespace XrEditor
{
    public class WpfUIElement : IEditorUIElement
    {
        private readonly FrameworkElement _control;

        public WpfUIElement(FrameworkElement control)
        {
            _control = control;
        }

        public void ScrollToView()
        {
            _control.BringIntoView();
        }
    }
}
