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
            DisplayName = displayName ?? value?.ToString() ?? "";
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
            _items = [];
            ValueType = typeof(object);
        }

        public object? SelectedValue
        {
            get => _selectedValue;
            set
            {
                if (Equals(_selectedValue, value))
                    return;

                if (value != null && !ValueType.IsAssignableFrom(value.GetType()))
                    return;

                if (!AllowNull && value == null)
                {
                    if (_items.Count > 0)
                        _selectedValue = _items[0].Value;
                }
                else
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

                var curSelection = _selectedValue;

                OnPropertyChanged(nameof(Items));

                if (_items.Any(a => a.Value == curSelection))
                    SelectedValue = curSelection;

            }
        }

        public Type ValueType { get; set; }

        public bool AllowNull { get; set; }
    }
}
