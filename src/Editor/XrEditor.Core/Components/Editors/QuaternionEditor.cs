using System.Numerics;
using UI.Binding;

namespace XrEditor
{
    public class QuaternionEditor : BaseEditor<Quaternion, Quaternion>
    {
        private int _suspendUpdate;
        private IValueScale _scale;

        public QuaternionEditor()
            : this(null, new ValueScale() { ScaleStep = 0.1f, ScaleSmallStep = 0.01f })
        {
        }

        public QuaternionEditor(IProperty<Quaternion> binding, float min, float max)
            : this(binding, new ValueScale() { ScaleMin = min, ScaleMax = max })
        {

        }

        public QuaternionEditor(IProperty<Quaternion>? binding, IValueScale scale)
        {
            _scale = scale;

            X = new FloatEditor() { Scale = _scale };
            Y = new FloatEditor() { Scale = _scale };
            Z = new FloatEditor() { Scale = _scale };
            W = new FloatEditor() { Scale = _scale };

            Binding = binding;

            X.ValueChanged += e =>
            {
                if (_suspendUpdate > 0 || Binding == null)
                    return;
                var curValue = Binding.Value;
                EditValue = new Quaternion(X.EditValue, curValue.Y, curValue.Z, curValue.W);
            };

            Y.ValueChanged += e =>
            {
                if (_suspendUpdate > 0 || Binding == null)
                    return;
                var curValue = Binding.Value;
                EditValue = new Quaternion(curValue.X, Y.EditValue, curValue.Z, curValue.W);
            };

            Z.ValueChanged += e =>
            {
                if (_suspendUpdate > 0 || Binding == null)
                    return;
                var curValue = Binding.Value;
                EditValue = new Quaternion(curValue.X, curValue.Y, Z.EditValue, curValue.W);
            };

            W.ValueChanged += e =>
            {
                if (_suspendUpdate > 0 || Binding == null)
                    return;
                var curValue = Binding.Value;
                EditValue = new Quaternion(curValue.X, curValue.Y, curValue.Z, W.EditValue);
            };
        }


        protected override void OnEditValueChanged(Quaternion newValue)
        {
            _suspendUpdate++;

            X._isLoading++;
            Y._isLoading++;
            Z._isLoading++;
            W._isLoading++;
            try
            {
                X.EditValue = newValue.X;
                Y.EditValue = newValue.Y;
                Z.EditValue = newValue.Z;
                W.EditValue = newValue.W;
            }
            finally
            {
                _suspendUpdate--;
                X._isLoading--;
                Y._isLoading--;
                Z._isLoading--;
                W._isLoading--;
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
                W.Scale = value;
            }
        }

        public FloatEditor X { get; }

        public FloatEditor Y { get; }

        public FloatEditor Z { get; }

        public FloatEditor W { get; }
    }
}
