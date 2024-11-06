using System.Collections.ObjectModel;

namespace XrEditor
{
    public class BaseActionsView : BaseView
    {

        public ActionView AddToggle(string iconName, bool isActive, Action<bool>? onChanged = null)
        {
            var action = new ActionView();
            action.IsActive = isActive;

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

        public void AddSelector()
        {

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

        public SingleSelector AddEnumSelect<T>(T value, Action<T> setValue) where T : struct, Enum
        {
            var items = Enum.GetValues<T>().Select(x => new SelectorItem(x)).ToArray();
            return AddSelect(items, value, setValue);
        }

        public SingleSelector AddSelect<T>(IList<SelectorItem> items, T value, Action<T> setValue) where T : struct, Enum
        {
            var result = new SingleSelector
            {
                Items = items,
                SelectedValue = value
            };

            result.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SingleSelector.SelectedValue))
                    setValue((T)result.SelectedValue);
            };

            Items.Add(result);

            return result;
        }


        public ObservableCollection<IToolbarItem> Items { get; } = [];
    }
}
