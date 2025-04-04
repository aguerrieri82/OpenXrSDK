using System.Windows;
using System.Windows.Controls;

namespace XrEditor
{
    public class WpfUIElement : IEditorUIElement
    {
        protected readonly FrameworkElement _control;

        protected WpfUIElement(FrameworkElement control)
        {
            _control = control;
        }

        public void ScrollToView()
        {
            _control.BringIntoView();
        }

        public static WpfUIElement? Create(FrameworkElement element)
        {
            if (element is ItemsControl itemsControl)
                return new WpfUIElementContainer(itemsControl); 
            return new WpfUIElement(element);
        }
    }



    public class WpfUIElementContainer : WpfUIElement, IEditorUIContainer
    {
        int _updateCount;

        internal WpfUIElementContainer(ItemsControl control)
            : base(control)
        {
        }

        public void BeginUpdate()
        {
            _updateCount++;
            if (_updateCount == 1)
                _control.IsEnabled = false;
        }

        public void EndUpdate()
        {
            _updateCount--;
            if (_updateCount == 0)
                _control.IsEnabled = true;
        }

        public void ScrollToView(object item)
        {
            if (_control is ListBox listBox)
                listBox.ScrollIntoView(item);
        }
    }
}