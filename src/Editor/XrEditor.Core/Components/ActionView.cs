using System.Reflection;
using XrEngine;

namespace XrEditor
{
    public class ActionView : BaseView, IToolbarItem
    {
        private bool _isActive;
        private bool _isEnabled;
        private IconView? _icon;
        private string? _displayName;

        public ActionView()
        {
            _isEnabled = true;
        }

        public ActionView(Action action, IDispatcher? dispatcher = null)
            : this(action.ToTask(), dispatcher)
        {
        }

        public ActionView(Func<Task> action, IDispatcher? dispatcher = null)
        {
            ExecuteCommand = new Command(async () =>
            {
                var wasActive = _isActive;
                IsActive = true;
                try
                {
                    if (dispatcher != null)
                        await dispatcher.ExecuteAsync(action);
                    else
                        await action();
                }
                finally
                {
                    IsActive = wasActive;
                }
            });
            _isEnabled = true;
        }



        public static void CreateActions(object obj, IList<ActionView> actions)
        {
            CreateActions(obj, obj.GetType(), null, actions);
        }

        public static void CreateActions(object obj, Type objType, object? host, IList<ActionView> actions)
        {
            foreach (var method in objType.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance))
            {
                var action = method.GetCustomAttribute<ActionAttribute>();
                if (action == null)
                    continue;

                var name = method.Name;
                if (name.EndsWith("Async"))
                    name = name[..^5];

                var propView = new ActionView
                {
                    DisplayName = name,
                    ExecuteCommand = new Command(() => method.Invoke(obj, null)),
                };

                actions.Add(propView);
            }
        }

        public Command? ExecuteCommand { get; set; }

        public string? DisplayName
        {
            get => _displayName;
            set
            {
                if (_displayName == value)
                    return;
                _displayName = value;
                OnPropertyChanged(nameof(DisplayName));
            }
        }

        public IconView? Icon
        {
            get => _icon;
            set
            {
                if (_icon == value)
                    return;
                _icon = value;
                OnPropertyChanged(nameof(Icon));
            }
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled == value)
                    return;
                _isEnabled = value;
                OnPropertyChanged(nameof(IsEnabled));
            }
        }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive == value)
                    return;
                _isActive = value;
                OnPropertyChanged(nameof(IsActive));
            }
        }


        public string? Name { get; set; }
    }
}
