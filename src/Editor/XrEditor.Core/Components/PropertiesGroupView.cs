using System.Windows.Input;

namespace XrEditor
{
    public enum PropertiesGroupType
    {
        Main,
        Inner
    }

    public class PropertiesGroupView : BaseView
    {
        private bool _isCollapsed;
        private object? _header;
        private IList<PropertyView> _properties = [];
        private IList<PropertiesGroupView> _groups = [];
        private IList<ActionView> _actions = [];

        public PropertiesGroupView(PropertiesGroupType groupType)
        {
            ToggleCollapseCommand = new Command(() => IsCollapsed = !IsCollapsed);
            GroupType = groupType;
        }

        public object? Header
        {
            get => _header;
            set
            {
                if (_header == value)
                    return;
                _header = value;
                OnPropertyChanged(nameof(Header));
            }
        }


        public bool IsCollapsed
        {
            get => _isCollapsed;
            set
            {
                if (_isCollapsed == value)
                    return;
                _isCollapsed = value;
                OnPropertyChanged(nameof(IsCollapsed));
            }
        }

        public IList<PropertyView> Properties
        {
            get => _properties;
            set
            {
                if (_properties == value)
                    return;
                _properties = value;
                OnPropertyChanged(nameof(Properties));
            }
        }

        public IList<PropertiesGroupView> Groups
        {
            get => _groups;
            set
            {
                if (_groups == value)
                    return;
                _groups = value;
                OnPropertyChanged(nameof(Groups));
            }
        }

        public IList<ActionView> Actions
        {
            get => _actions;
            set
            {
                if (_actions == value)
                    return;
                _actions = value;
                OnPropertyChanged(nameof(Actions));
            }
        }

        public PropertiesGroupType GroupType { get; }

        public ICommand ToggleCollapseCommand { get; }

        public INode Node { get; set; }
    }
}
