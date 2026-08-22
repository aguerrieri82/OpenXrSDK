using System.Windows.Input;

namespace XrEditor
{
    public enum PropertiesGroupType
    {
        Main,
        Inner
    }

    public class PropertiesGroupView : BaseView, IDisposable
    {
        private bool _isCollapsed;
        private object? _header;
        private IList<PropertyView> _properties = [];
        private IList<PropertiesGroupView> _groups = [];
        private IList<ActionView> _actions = [];
        private PropertiesPresetView[] _presets = [];
        private PropertiesPresetView? _selectedPreset;
        private string? _presetName;

        public PropertiesGroupView(PropertiesGroupType groupType)
        {
            ToggleCollapseCommand = new Command(() => IsCollapsed = !IsCollapsed);
            SavePresetCommand = new Command(SavePreset);
            ApplyPresetCommand = new Command(ApplyPreset);
            DeletePresetCommand = new Command(DeletePreset);
            GroupType = groupType;

            UpdateState();
        }

        public void SavePreset()
        {
            var name = _selectedPreset?.Name;

            if (name != _presetName)
                name = _presetName;

            if (string.IsNullOrWhiteSpace(name))
                name = "new-preset-" + (_presets.Length + 1);

            if (Node is IPresetManager pm)
                pm.SavePreset(name);

            RefreshPresets();

            _selectedPreset = _presets.FirstOrDefault(a => a.Name == name);

            OnPropertyChanged(nameof(SelectedPreset));

            UpdateState();
        }

        public void DeletePreset()
        {
            if (Node is IPresetManager pm && _selectedPreset != null)
            {
                pm.DeletePreset(_selectedPreset.Value);
                RefreshPresets();
                UpdateState();
            }
        }

        public void ApplyPreset()
        {
            if (_selectedPreset != null)
                LoadPreset(_selectedPreset.Value);
        }

        public void LoadPreset(ComponentPreset preset)
        {
            if (Node is IPresetManager pm)
                pm.LoadPreset(preset);

            void RefreshGroup(PropertiesGroupView group)
            {
                foreach (var prop in group.Properties)
                    prop.Editor?.NotifyBindValueChanged();

                foreach (var innerGroup in group.Groups)
                    RefreshGroup(innerGroup);
            }

            RefreshGroup(this);

        }

        public void RefreshPresets()
        {
            if (Node is not IPresetManager pm)
                return;

            var presets = pm.ListPresets();

            var newValue = new List<PropertiesPresetView>();

            foreach (var preset in presets)
                newValue.Add(new PropertiesPresetView(preset));

            Presets = newValue.ToArray();

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

        public PropertiesPresetView? SelectedPreset
        {
            get => _selectedPreset;
            set
            {
                if (_selectedPreset == value)
                    return;

                _selectedPreset = value;

                OnPropertyChanged(nameof(SelectedPreset));

                UpdateState();
                ApplyPreset();
            }
        }

        public PropertiesPresetView[] Presets
        {
            get => _presets;
            set
            {
                if (_presets == value)
                    return;
                _presets = value;
                OnPropertyChanged(nameof(Presets));
            }
        }

        public string? PresetName
        {
            get => _presetName;
            set
            {
                if (_presetName == value)
                    return;
                _presetName = value;

                OnPropertyChanged(nameof(PresetName));

                UpdateState();
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

        protected void UpdateState()
        {
            DeletePresetCommand.IsEnabled = _selectedPreset != null;
            ApplyPresetCommand.IsEnabled = _selectedPreset != null;
            SavePresetCommand.IsEnabled = !string.IsNullOrWhiteSpace(_presetName);
        }

        public void Dispose()
        {
            foreach (var item in _groups)
                item.Dispose();

            foreach (var item in _properties)
                item.Dispose();

            GC.SuppressFinalize(this);
        }

        public Command SavePresetCommand { get; }

        public Command ApplyPresetCommand { get; }

        public Command DeletePresetCommand { get; }

        public PropertiesGroupType GroupType { get; }

        public ICommand ToggleCollapseCommand { get; }

        public PropertiesGroupView? Parent { get; internal set; }

        public INode? Node { get; set; }
    }
}
