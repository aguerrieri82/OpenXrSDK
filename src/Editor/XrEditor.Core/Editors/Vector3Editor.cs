using CanvasUI;
using System.Numerics;
using UI.Binding;

namespace XrEditor
{
    public class Vector3Editor : BaseEditor<Vector3, Vector3>
    {
        private int _suspendUpdate;
        private IValueScale _scale;

        public Vector3Editor()
            : this(null, new ValueScale())
        {
        }

        public Vector3Editor(IProperty<Vector3> binding, float min, float max)
            : this(binding, new ValueScale() { ScaleMin = min, ScaleMax = max })
        {

        }

        public Vector3Editor(IProperty<Vector3>? binding, IValueScale scale)
        {
            _scale = scale;

            X = new FloatEditor() { Scale = _scale }; 
            Y = new FloatEditor() { Scale = _scale };
            Z = new FloatEditor() { Scale = _scale };

            Binding = binding;

            X.ValueChanged += e =>
            {
                if (_suspendUpdate > 0 || Binding == null)
                    return;
                var curValue = Binding.Value;
                EditValue = new Vector3(X.EditValue, curValue.Y, curValue.Z);
            };

            Y.ValueChanged += e =>
            {
                if (_suspendUpdate > 0 || Binding == null)
                    return;
                var curValue = Binding.Value;
                EditValue = new Vector3(curValue.X, Y.EditValue, curValue.Z);
            };

            Z.ValueChanged += e =>
            {
                if (_suspendUpdate > 0 || Binding == null)
                    return;
                var curValue = Binding.Value;
                EditValue = new Vector3(curValue.X, curValue.Y, Z.EditValue);
            };
        }


        protected override void OnEditValueChanged(Vector3 newValue)
        {
            _suspendUpdate++;
            try
            {
                X.EditValue = newValue.X;
                Y.EditValue = newValue.Y;
                Z.EditValue = newValue.Z;
            }
            finally
            {
                _suspendUpdate--;
            }

            base.OnEditValueChanged(newValue);
        }

        public IValueScale Scale
        {
            get => _scale;
            set
            {
                _scale = value;
                X.Scale = value;
                Y.Scale = value;
                Z.Scale = value;
            }
        }

        public FloatEditor X { get; }

        public FloatEditor Y { get; }

        public FloatEditor Z { get; }
    }
}
