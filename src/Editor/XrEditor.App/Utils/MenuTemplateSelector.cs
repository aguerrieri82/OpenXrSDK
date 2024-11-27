using System.Windows;
using System.Windows.Controls;

namespace XrEditor
{
    public class MenuTemplateSelector : ItemContainerTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, ItemsControl parentItemsControl)
        {
            if (item is ActionDivider)
                return (DataTemplate)parentItemsControl.FindResource("ActionDivider");

            if (item is ActionView)
                return (DataTemplate)parentItemsControl.FindResource("ActionView");

            if (item is MenuView)
                return (DataTemplate)parentItemsControl.FindResource("MenuView");

            return base.SelectTemplate(item, parentItemsControl);
        }
    }
}
