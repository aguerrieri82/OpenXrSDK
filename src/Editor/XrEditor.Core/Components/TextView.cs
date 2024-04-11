namespace XrEditor
{
    public class TextView : BaseView, IToolbarItem
    {
        private string? _text;

        public string? Text
        {
            get => _text;
            set
            {
                if (_text == value)
                    return;
                _text = value;
                OnPropertyChanged(nameof(Text));
            }
        }

    }
}
