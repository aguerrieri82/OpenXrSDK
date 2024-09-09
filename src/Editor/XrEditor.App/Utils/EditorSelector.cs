using System.Windows;
using System.Windows.Controls;

namespace XrEditor
{
    public class EditorSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is IEnumEditor)
                return (DataTemplate)Application.Current.FindResource("EnumEditor");
            if (item is ITextEditor)
                return (DataTemplate)Application.Current.FindResource("TextEditor");
            return base.SelectTemplate(item, container);
        }
    }
}
