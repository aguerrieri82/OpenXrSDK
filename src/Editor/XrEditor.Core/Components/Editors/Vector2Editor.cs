using System.Numerics;
using UI.Binding;

namespace XrEditor
{
    public class Vector2Editor : BaseEditor<Vector2, Vector2>
    {
        private int _suspendUpdate;
        private IValueScale _scale;
        private bool _isLocked;

        public Vector2Editor()
            : this(null, new ValueScale() { ScaleStep = 0.1f, ScaleSmallStep = 0.01f })
        {
        }

        public Vector2Editor(IProperty<Vector2> binding, float min, float max)
            : this(binding, new ValueScale() { ScaleMin = min, ScaleMax = max })
        {

        }

        public Vector2Editor(IProperty<Vector2>? binding, IValueScale scale)
        {
            _scale = scale;

            X = new FloatEditor() { Scale = _scale };
            Y = new FloatEditor() { Scale = _scale };


            Binding = binding;

            X.ValueChanged += e =>
            {
                if (_suspendUpdate > 0 || Binding == null)
                    return;
                var curValue = Binding.Value;
                if (_isLocked)
                    EditValue = new Vector2(X.EditValue, X.EditValue);
                else
                    EditValue = new Vector2(X.EditValue, curValue.Y);
            };

            Y.ValueChanged += e =>
            {
                if (_suspendUpdate > 0 || Binding == null)
                    return;
                var curValue = Binding.Value;
                if (_isLocked)
                    EditValue = new Vector2(Y.EditValue, Y.EditValue);
                else
                    EditValue = new Vector2(curValue.X, Y.EditValue);
            };
        }


        protected override void OnEditValueChanged(Vector2 newValue)
        {
            _suspendUpdate++;

            X._isLoading++;
            Y._isLoading++;


            try
            {
                X.EditValue = newValue.X;
                Y.EditValue = newValue.Y;

            }
            finally
            {
                _suspendUpdate--;
                X._isLoading--;
                Y._isLoading--;

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

    }
}
