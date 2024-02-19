﻿using OpenXr.Engine;

namespace Xr.Engine.Editor
{
    public class PropertiesEditor : BasePanel
    {
        private EngineObject? _activeObject;
        private IList<PropertyView>? _properties;

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
                result.Add(new PropertyView
                {
                    Label = "Pivot",
                    Editor = new Vector3Editor(() => obj3d.Transform.Pivot, value => obj3d.Transform.Pivot = value, -2f, 2f)
                });
                result.Add(new PropertyView
                {
                    Label = "Scale",
                    Editor = new Vector3Editor(() => obj3d.Transform.Scale, value => obj3d.Transform.Scale = value, 0.01f, 5f)
                });
                result.Add(new PropertyView
                {
                    Label = "Position",
                    Editor = new Vector3Editor(() => obj3d.Transform.Position, value => obj3d.Transform.Position = value, -3, 3f)
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
    }
}
