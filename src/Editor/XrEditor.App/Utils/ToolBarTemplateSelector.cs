using System.Windows;
using System.Windows.Controls;

namespace XrEditor
{
    public class ToolBarTemplateSelector : ItemContainerTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, ItemsControl parentItemsControl)
        {
            return base.SelectTemplate(item, parentItemsControl);
        }
    }
}
