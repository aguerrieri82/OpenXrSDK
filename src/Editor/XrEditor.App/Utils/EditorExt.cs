using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using XrEditor.Abstraction;
using CefSharp.DevTools.DOMSnapshot;

namespace XrEditor
{
    public static class EditorExt
    {

        public static readonly DependencyProperty ControlProperty =
            DependencyProperty.RegisterAttached(
                "Container",       
                typeof(IEditorUIElementContainer),                    
                typeof(EditorExt),            
                new PropertyMetadata(null, OnContainerChanged)); // Default value and callback

        public static void OnContainerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != null && d is FrameworkElement uiElement)
            {
                var container = (IEditorUIElementContainer)e.NewValue;
                container.UIElement = new WpfUIElement(uiElement);
            }
        }


        public static bool GetContainer(UIElement element)
        {
            return (bool)element.GetValue(ControlProperty);
        }


        public static void SetContainer(UIElement element, IEditorUIElement value)
        {
            element.SetValue(ControlProperty, value);
        }

    }
}
