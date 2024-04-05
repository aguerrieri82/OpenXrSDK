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
            return base.SelectTemplate(item, container);
        }
    }
}
