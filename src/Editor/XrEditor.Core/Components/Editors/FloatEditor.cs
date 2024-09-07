using UI.Binding;
using XrEngine;

namespace XrEditor
{
    public class FloatEditor : BaseEditor<float, float>
    {
        private IValueScale _scale;

        public FloatEditor()
        {
            _scale = new ValueScale();
        }

        public FloatEditor(IProperty<float> binding, IValueScale scale)
        {
            _scale = scale;
            Binding = binding;
        }

        public FloatEditor(IProperty<float> binding, float min, float max, float step = 1f)
        {
            _scale = new ValueScale()
            {
                ScaleMin = min,
                ScaleMax = max,
                ScaleSmallStep = step,
                ScaleStep = step
            };

            Binding = binding;
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

        protected override void OnEditValueChanged(float newValue)
        {
            OnPropertyChanged(nameof(ScaleValue));
            base.OnEditValueChanged(newValue);
        }


        public Func<float, string?> ScaleFormat => _scale.Format;
    }

    public class FloatEditorFactory : IPropertyEditorFactory
    {
        public bool CanHandle(Type type)
        {
            return type == typeof(float);
        }

        public IPropertyEditor CreateEditor(Type type, IEnumerable<Attribute> attributes)
        {
            var range = attributes.OfType<RangeAttribute>().FirstOrDefault();
            var result = new FloatEditor();
            if (range != null)
            {
                result.Scale = new ValueScale()
                {
                    ScaleMin = range.Min,
                    ScaleMax = range.Max,
                    ScaleStep = range.Step,
                    ScaleSmallStep = range.Step
                };
            }
            return result;
        }
    }
}
