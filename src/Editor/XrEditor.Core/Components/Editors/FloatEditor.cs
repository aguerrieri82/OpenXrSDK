using Silk.NET.Direct3D11;
using UI.Binding;
using XrEngine;
using ValueType = XrEngine.ValueType;

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

        public FloatEditor(IProperty<float> binding, float min = 0, float max = 0, float step = 1f)
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


        public override void SetAttributes(IEnumerable<Attribute> attributes)
        {
            var range = attributes.OfType<RangeAttribute>().FirstOrDefault();

            var valueType = attributes.OfType<ValueTypeAttribute>().FirstOrDefault()?.Type ?? ValueType.None;

            if (valueType == ValueType.Radiant)
                Scale = RadDegreeScale.Instance;
            else
            {
                if (range == null)
                {
                    Scale = new ValueScale
                    {
                        ScaleMin = 0,
                        ScaleMax = 1,
                        ScaleStep = 0.1f,
                        ScaleSmallStep = 0.1f,
                    };
                }
                else
                {
                    Scale = new ValueScale()
                    {
                        ScaleMin = range.Min,
                        ScaleMax = range.Max,
                        ScaleStep = range.Step,
                        ScaleSmallStep = range.Step
                    };
                }

            }
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

}
