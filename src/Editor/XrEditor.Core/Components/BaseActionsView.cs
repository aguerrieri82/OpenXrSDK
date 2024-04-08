using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEditor
{
    public class BaseActionsView : BaseView 
    {

        public ActionView AddToggle(string iconName, Action<bool>? onChanged = null)
        {
            var action = new ActionView();
            
            action.ExecuteCommand = new Command(() =>
            {
                action.IsActive = !action.IsActive;
                onChanged?.Invoke(action.IsActive);
            });

            action.Icon = new IconView { Name = iconName };
            Items.Add(action);
            return action;
        }

        public ActionView AddButton(string iconName, Action action, string? displayName = null)
        {
            var result = new ActionView(action)
            {
                Icon = new IconView()
                {
                    Name = iconName
                },
                DisplayName = displayName
            };

            Items.Add(result);

            return result;
        }

        public void AddDivider()
        {
            Items.Add(new ActionDivider());
        }

        public TextView AddText(string text)
        {
            var result = new TextView();
            result.Text = text;
            Items.Add(result);
            return result;
        }


        public ObservableCollection<IToolbarItem> Items { get; } = [];
    }
}
