using Newtonsoft.Json.Linq;

using System.Numerics;
using UI.Binding;
using XrEngine.OpenGL;
using XrMath;

namespace XrEditor
{
    public class ColorEditor : BaseEditor<Color, Color>
    {
        public ColorEditor()
        {
        }

        public ColorEditor(IProperty<Color> binding)
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

    }
}
