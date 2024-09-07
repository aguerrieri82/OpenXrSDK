using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace XrEditor
{
    public class WpfUIElement : IEditorUIElement
    {
        private FrameworkElement _control;

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
