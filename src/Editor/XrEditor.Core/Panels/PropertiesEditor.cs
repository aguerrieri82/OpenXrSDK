using System.Numerics;
using XrEngine;

namespace XrEditor
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


            if (ActiveObject is PbrMaterial pbrMat)
            {

                result.Add(new PropertyView
                {
                    Label = "Roughness",
                    Editor = new FloatEditor(() => pbrMat.MetallicRoughness!.RoughnessFactor, value => 
                    { 
                        pbrMat.MetallicRoughness!.RoughnessFactor = value; 
                        pbrMat.NotifyChanged(ObjectChangeType.Render); 
                    }, 0, 1f)
                });
                result.Add(new PropertyView
                {
                    Label = "Metallic",
                    Editor = new FloatEditor(() => pbrMat.MetallicRoughness!.MetallicFactor, value =>
                    {
                        pbrMat.MetallicRoughness!.MetallicFactor = value;
                        pbrMat.MetallicRoughness!.BaseColorFactor = new XrMath.Color(value, 1, 0, 1);
                        pbrMat.NotifyChanged(ObjectChangeType.Render);
                    }, 0, 1f)
                });


          
            }

            var light = EngineApp.Current?.ActiveScene?.Descendants<ImageLight>().FirstOrDefault();

            if (light != null)
            {
                result.Add(new PropertyView
                {
                    Label = "Light Intensity",
                    Editor = new FloatEditor(() => light.Intensity, value =>
                    {
                        light.Intensity = value;
                        light.NotifyChanged(ObjectChangeType.Render);
                    }, 0f, 50000f)
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
