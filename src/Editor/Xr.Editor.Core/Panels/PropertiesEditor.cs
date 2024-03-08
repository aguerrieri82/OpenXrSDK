using System.Numerics;
using Xr.Engine;

namespace Xr.Editor
{
    public class PropertiesEditor : BasePanel
    {
        private EngineObject? _activeObject;
        private IList<PropertyView>? _properties;

        public PropertiesEditor()
        {
            Instance = this;
        }

        public EngineObject? ActiveObject
        {
            get => _activeObject;
            set
            {
                if (_activeObject == value)
                    return;
                _activeObject = value;
                OnPropertyChanged(nameof(ActiveObject));
                UpdateProperties();
            }
        }

        protected void UpdateProperties()
        {
            var result = new List<PropertyView>();

            if (ActiveObject is Object3D obj3d)
            {
                void SetPivot(Vector3 value)
                {
                    obj3d.Transform.SetLocalPivot(value, false);
                    _properties?.First(a => a.Label == "Position")?.Editor?.NotifyValueChanged();
                }

                result.Add(new PropertyView
                {
                    Label = "Pivot",
                    Editor = new Vector3Editor(() => obj3d.Transform.LocalPivot, SetPivot, -2f, 2f)
                });
                result.Add(new PropertyView
                {
                    Label = "Scale",
                    Editor = new Vector3Editor(() => obj3d.Transform.Scale, value => obj3d.Transform.Scale = value, 0.01f, 5f)
                });
                result.Add(new PropertyView
                {
                    Label = "Position",
                    Editor = new Vector3Editor(() => obj3d.Transform.Position, value => obj3d.Transform.Position = value, -5f, 5f)
                });
                result.Add(new PropertyView
                {
                    Label = "Rotation",
                    Editor = new Vector3Editor(() => obj3d.Transform.Rotation, value => obj3d.Transform.Rotation = value, -MathF.PI, MathF.PI)
                });
            }

            Properties = result;
        }

        public IList<PropertyView>? Properties
        {
            get => _properties;
            set
            {
                _properties = value;
                OnPropertyChanged(nameof(Properties));
            }
        }

        public static PropertiesEditor? Instance { get; internal set; }
    }
}
