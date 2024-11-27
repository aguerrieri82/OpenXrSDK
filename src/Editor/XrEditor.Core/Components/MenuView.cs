namespace XrEditor
{
    public class MenuView : BaseActionsView, IToolbarItem
    {
        private string? _displayName;
        private IconView? _icon;

        public MenuView AddGroup(string displayName)
        {
            var result = new MenuView() { DisplayName = displayName };
            Items.Add(result);
            return result;
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
    }
}
