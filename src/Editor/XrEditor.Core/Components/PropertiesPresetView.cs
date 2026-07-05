using System.Windows.Input;

namespace XrEditor
{

    public class PropertiesPresetView : BaseView
    {
        private string _name;
        private readonly ComponentPreset _value;

        public PropertiesPresetView(ComponentPreset value)
        {
            _name = value.Name!;
            _value = value;
        }

        public string Name
        {
            get => _name;
            set
            {
                if (_name == value)
                    return;
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }


        public ComponentPreset Value => _value;
    }
}
