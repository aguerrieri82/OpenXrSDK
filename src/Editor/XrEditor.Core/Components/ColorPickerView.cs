namespace XrEditor
{
    public class ColorPickerView : BaseView
    {
        private string? _selectedColor;

        public string? SelectedColor
        {
            get => _selectedColor;
            set
            {
                if (_selectedColor == value)
                    return;
                _selectedColor = value;
                OnPropertyChanged(nameof(SelectedColor));
            }
        }

    }
}
