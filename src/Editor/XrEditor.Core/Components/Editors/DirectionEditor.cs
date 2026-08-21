using System.Numerics;
using UI.Binding;
using XrMath;

namespace XrEditor
{
    public class DirectionEditor : Vector3Editor
    {
        public class ValuePresetView
        {
            readonly DirectionEditor _editor;

            public ValuePresetView(string name, Vector3 value, DirectionEditor editor)
            {
                Name = name;
                Value = value;
                _editor = editor;
                ExecuteCommand = new Command(Execute);
            }

            public void Execute()
            {
                _editor.EditValue = Value;
            }

            public Command ExecuteCommand { get; }

            public Vector3 Value { get; }

            public string Name { get; }
        }

        public DirectionEditor()
            : this(null)
        {
        }

        public DirectionEditor(IProperty<Vector3>? binding)
            : base(binding, new ValueScale() { ScaleStep = 0.1f, ScaleSmallStep = 0.01f, ScaleMin = -1, ScaleMax = 1 })
        {
            Presets = [
                new ValuePresetView("X+", new Vector3(1,0,0), this),
                new ValuePresetView("X-", new Vector3(-1,0,0), this),
                new ValuePresetView("Y+", new Vector3(0,1,0), this),
                new ValuePresetView("Y-", new Vector3(0,-1,0), this),
                new ValuePresetView("Z+", new Vector3(0,0,1), this),
                new ValuePresetView("Z-", new Vector3(0,0,-1), this)
             ];

            Azimuth = new FloatEditor() { Scale = RadDegreeScale.Instance };
            Altitude = new FloatEditor() { Scale = RadDegreeScale.Instance };

            Azimuth.ValueChanged += OnPolarChanged;
            Altitude.ValueChanged += OnPolarChanged;
        }

        protected void OnPolarChanged(IPropertyEditor editor)
        {
            if (_suspendUpdate > 0 || Binding == null)
                return;

            var azimuth = Azimuth.EditValue;
            var altitude = Altitude.EditValue;

            var horizontal = MathF.Cos(altitude);

            var value = new Vector3(
                MathF.Sin(azimuth) * horizontal,
                MathF.Sin(altitude),
                -MathF.Cos(azimuth) * horizontal);

            EditValue = value.Normalize();
        }

        protected override void OnBindValueChanged(Vector3 newValue)
        {
            var direction = Vector3.Normalize(newValue);

            _suspendUpdate++;

            Altitude.EditValue = MathF.Asin(direction.Y);

            Azimuth.EditValue = MathF.Atan2(direction.X, -direction.Z);

            _suspendUpdate--;

            base.OnBindValueChanged(newValue);
        }

        public FloatEditor Azimuth { get; }

        public FloatEditor Altitude { get; }

        public ValuePresetView[] Presets { get; }
    }
}
