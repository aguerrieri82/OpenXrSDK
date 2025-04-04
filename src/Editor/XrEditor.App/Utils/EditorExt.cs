using System.Windows;
using XrEditor.Abstraction;

namespace XrEditor
{
    public static class EditorExt
    {

        public static readonly DependencyProperty ControlProperty =
            DependencyProperty.RegisterAttached(
                "Container",
                typeof(IEditorUIElementHost),
                typeof(EditorExt),
                new PropertyMetadata(null, OnContainerChanged)); // Default value and callback

        public static void OnContainerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != null && d is FrameworkElement uiElement)
            {
                var container = (IEditorUIElementHost)e.NewValue;
                container.UIElement = WpfUIElement.Create(uiElement);
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
