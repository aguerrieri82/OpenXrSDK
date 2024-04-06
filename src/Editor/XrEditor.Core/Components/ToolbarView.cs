
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEditor
{
    public class ToolbarView : BaseView
    {

        public ActionView AddToggle(string iconName)
        {
            var action = new ActionView();
            action.Execute = new Command(() => action.IsActive = !action.IsActive);
            action.Icon = new IconView { Name = iconName };
            Items.Add(action);  
            return action;
        }

        public ActionView AddButton(string iconName, Action action)
        {
            var result = new ActionView(action) 
            { 
                Icon = new IconView() 
                { 
                    Name = iconName 
                } 
            };
            
            Items.Add(result);

            return result;
        }

        public ObservableCollection<IToolbarItem> Items { get; } = [];
    }
}
