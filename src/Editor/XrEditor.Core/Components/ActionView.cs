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

        public ActionView(Action action)
        {
            ExecuteCommand = new Command(action);
            _isEnabled = true;
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
    }
}
