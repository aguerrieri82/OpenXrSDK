using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            return base.SelectTemplate(item, parentItemsControl);
        }
    }
}
