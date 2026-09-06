using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using UI.Binding;

namespace XrEditor
{
    public class Vector3Editor : BaseEditor<Vector3, Vector3>
    {
        protected int _suspendUpdate;
        protected IValueScale _scale;
        protected bool _isLocked;

        public Vector3Editor()
            : this(null)
        {
        }

        public Vector3Editor(IProperty<Vector3> binding, float min, float max)
            : this(binding, new ValueScale() { ScaleMin = min, ScaleMax = max })
        {
        }

        public Vector3Editor(IProperty<Vector3>? binding, IValueScale? scale = null)
        {
            X = new FloatEditor();
            Y = new FloatEditor();
            Z = new FloatEditor();

            Scale = scale ?? new ValueScale() { ScaleStep = 0.1f, ScaleSmallStep = 0.01f };

            Binding = binding;

            X.ValueChanged += e =>
            {
                if (_suspendUpdate > 0 || Binding == null)
                    return;
                var curValue = Binding.Value;
                if (_isLocked)
                    EditValue = new Vector3(X.EditValue, X.EditValue, X.EditValue);
                else
                    EditValue = new Vector3(X.EditValue, curValue.Y, curValue.Z);
            };

            Y.ValueChanged += e =>
            {
                if (_suspendUpdate > 0 || Binding == null)
                    return;
                var curValue = Binding.Value;
                if (_isLocked)
                    EditValue = new Vector3(Y.EditValue, Y.EditValue, Y.EditValue);
                else
                    EditValue = new Vector3(curValue.X, Y.EditValue, curValue.Z);
            };

            Z.ValueChanged += e =>
            {
                if (_suspendUpdate > 0 || Binding == null)
                    return;
                var curValue = Binding.Value;
                if (_isLocked)
                    EditValue = new Vector3(Z.EditValue, Z.EditValue, Z.EditValue);
                else
                    EditValue = new Vector3(curValue.X, curValue.Y, Z.EditValue);
            };
        }

        public override void SetAttributes(IEnumerable<Attribute> attributes)
        {
            X.SetAttributes(attributes);
            Y.SetAttributes(attributes);
            Z.SetAttributes(attributes);
        }

        protected override void OnEditValueChanged(Vector3 newValue)
        {
            _suspendUpdate++;

            X._isLoading++;
            Y._isLoading++;
            Z._isLoading++;

            try
            {
                X.EditValue = newValue.X;
                Y.EditValue = newValue.Y;
                Z.EditValue = newValue.Z;
            }
            finally
            {
                _suspendUpdate--;
                X._isLoading--;
                Y._isLoading--;
                Z._isLoading--;
            }

            base.OnEditValueChanged(newValue);
        }

        public IValueScale Scale
        {
            get => _scale;
            [MemberNotNull(nameof(_scale))]
            set
            {
                _scale = value;
                X.Scale = value;
                Y.Scale = value;
                Z.Scale = value;
            }
        }

        public bool LockedVisible { get; set; }

        public bool IsLocked
        {
            get => _isLocked;
            set
            {
                if (value == _isLocked)
                    return;
                _isLocked = value;
                OnPropertyChanged(nameof(IsLocked));
            }
        }

        public FloatEditor X { get; }

        public FloatEditor Y { get; }

        public FloatEditor Z { get; }
    }
}
