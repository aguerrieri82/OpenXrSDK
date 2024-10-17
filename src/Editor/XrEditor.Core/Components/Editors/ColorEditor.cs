using UI.Binding;
using XrEngine;
using XrMath;

namespace XrEditor
{
    public class ColorEditor : BaseEditor<Color, Color>
    {
        private IPopup? _popup;

        public ColorEditor()
        {
            ShowPickerCommand = new Command(ShowPicker);
        }

        public ColorEditor(IProperty<Color> binding)
            : this()
        {
            Binding = binding;
        }

        protected override void OnEditValueChanged(Color newValue)
        {
            OnPropertyChanged("HexValue");
            base.OnEditValueChanged(newValue);
        }

        public string HexValue
        {
            get => _editValue.ToHex();
            set
            {
                EditValue = Color.Parse(value);
            }
        }


        async void ShowPicker()
        {
            if (_popup != null)
                return;

            var manager = Context.Require<IPanelManager>();

            var oldColor = _editValue;

            var body = new ColorPickerView()
            {
                SelectedColor = HexValue
            };

            body.PropertyChanged += (s, e) =>
            {
                EditValue = body.SelectedColor;
            };

            var content = new ContentView()
            {
                Title = "Select color",
                Content = body,
                Actions = [
   
                    new ActionView
                    {
                        DisplayName = "Cancel",
                    },
                    new ActionView
                    {
                        DisplayName = "Select",
                    },
                ]
            };

            _popup = manager.CreatePopup(content, new Size2I(300, 500));
            
            var result = await _popup.ShowAsync();

            if (result == null || result.DisplayName == "Cancel")
                EditValue = oldColor;

            _popup = null; 
        }


        public Command ShowPickerCommand { get; }

    }
}
