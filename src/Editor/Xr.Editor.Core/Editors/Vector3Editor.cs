using System.Numerics;

namespace Xr.Editor
{
    public class Vector3Editor : BaseEditor<Vector3>
    {
        readonly Func<Vector3> _getter;
        readonly Action<Vector3> _setter;
        private int _suspendUpdate;

        public Vector3Editor(Func<Vector3> getter, Action<Vector3> setter, float min, float max)
        {
            X = new FloatEditor() { Max = max, Min = min };
            Y = new FloatEditor() { Max = max, Min = min };
            Z = new FloatEditor() { Max = max, Min = min };

            _getter = getter;

            _setter = setter;


            Value = getter();

            X.ValueChanged += (s, v) =>
            {
                if (_suspendUpdate > 0)
                    return;
                var curValue = _getter();
                Value = new Vector3(v, curValue.Y, curValue.Z);
            };

            Y.ValueChanged += (s, v) =>
            {
                if (_suspendUpdate > 0)
                    return;
                var curValue = _getter();
                Value = new Vector3(curValue.X, v, curValue.Z);
            };

            Z.ValueChanged += (s, v) =>
            {
                if (_suspendUpdate > 0)
                    return;
                var curValue = _getter();
                Value = new Vector3(curValue.X, curValue.Y, v);
            };
        }

        public override void NotifyValueChanged()
        {
            Value = _getter();
        }

        protected override void OnValueChanged(Vector3 newValue)
        {
            _setter(newValue);
            _suspendUpdate++;
            try
            {
                X.Value = newValue.X;
                Y.Value = newValue.Y;
                Z.Value = newValue.Z;
            }
            finally
            {
                _suspendUpdate--;
            }
        }

        public FloatEditor X { get; }

        public FloatEditor Y { get; }

        public FloatEditor Z { get; }
    }
}
