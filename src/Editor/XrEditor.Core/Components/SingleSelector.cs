namespace XrEditor
{
    public class SelectorItem
    {
        public SelectorItem()
        {
            DisplayName = "";
        }

        public SelectorItem(object? value, string? displayName = null)
        {
            Value = value;
            DisplayName = displayName ?? value.ToString();
        }

        public string DisplayName { get; set; }

        public object? Value { get; set; }
    }

    public class SingleSelector : BaseView, IToolbarItem
    {
        private object? _selectedValue;
        private IList<SelectorItem> _items;

        public SingleSelector()
        {
            _items = new List<SelectorItem>();
        }

        public object? SelectedValue
        {
            get => _selectedValue;
            set
            {
                if (Equals(_selectedValue, value))
                    return;
                _selectedValue = value;
                OnPropertyChanged(nameof(SelectedValue));
            }
        }

        public IList<SelectorItem> Items
        {
            get => _items;
            set
            {
                if (_items == value)
                    return;
                _items = value;
                OnPropertyChanged(nameof(Items));
            }
        }
    }
}
