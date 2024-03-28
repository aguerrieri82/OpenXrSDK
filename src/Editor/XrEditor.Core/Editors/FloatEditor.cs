using Newtonsoft.Json.Linq;
using System.Numerics;
using UI.Binding;
using XrEngine.OpenGL;

namespace XrEditor
{
    public class FloatEditor : BaseEditor<float, float>
    {
        private IValueScale _scale;

        public FloatEditor()
        {
            _scale = new ValueScale();
        }

        public float ScaleValue
        {
            get => _scale.ValueToScale(_editValue);
            set
            {
                if (Equals(ScaleValue, value))
                    return;
                EditValue = _scale.ScaleToValue(value);
                OnPropertyChanged(nameof(ScaleValue));
            }
        }

        public FloatEditor(IProperty<float> binding, float min, float max)
        {
            Binding = binding;

            _scale = new ValueScale
            {
                ScaleMin = min,
                ScaleMax = max
            };
        }


        public IValueScale Scale
        {
            get => _scale;
            set
            {
                if (_scale == value)
                    return;
                _scale = value;
                OnPropertyChanged(nameof(Scale));
            }
        }


        public Func<float, string?> ScaleFormat => _scale.Format;
    }
}
