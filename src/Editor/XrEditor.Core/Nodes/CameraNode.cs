using UI.Binding;
using XrEngine;

namespace XrEditor.Nodes
{
    public class CameraNode<T> : Object3DNode<T> where T : Camera
    {
        public CameraNode(T value) : base(value)
        {
            _autoGenProps = false;
        }

        protected override void EditorProperties(Binder<T> binder, IList<PropertyView> curProps)
        {
            // base.EditorProperties(binder, curProps);

            binder.PropertyChanged += (_, prop, _, _) =>
            {
                _value.NotifyChanged(ObjectChangeType.Render);
                if (prop.Name == nameof(PerspectiveCamera.FovDegree) || prop.Name == nameof(Camera.Near) || prop.Name == nameof(Camera.Far))
                    ((PerspectiveCamera)(object)_value).UpdateProjection();
            };

            curProps.Add(new PropertyView
            {
                Label = "Background Color",
                Editor = new ColorEditor(binder.Prop(a => a.BackgroundColor))
            });


            curProps.Add(new PropertyView
            {
                Label = "Exposure",
                Editor = new FloatEditor(binder.Prop(a => a.Exposure), 0, 5, 0.1f)
            });

            curProps.Add(new PropertyView
            {
                Label = "Far",
                Editor = new FloatEditor(binder.Prop(a => a.Far), 1, 180),
            });

            curProps.Add(new PropertyView
            {
                Label = "Near",
                Editor = new FloatEditor(binder.Prop(a => a.Near), 1, 180),
            });

            if (_value is PerspectiveCamera persp)
            {

                curProps.Add(new PropertyView
                {
                    Label = "Fov",
                    Editor = new FloatEditor(binder.Prop(a => (a as PerspectiveCamera)!.FovDegree), 0, 360, 1),
                });

                curProps.Add(new PropertyView
                {
                    Label = "ActiveEye",
                    Editor = new TextEditor<int>(a => int.Parse(a), a => a.ToString())
                    {
                        Binding = binder.Prop(a => (a as PerspectiveCamera)!.ActiveEye)
                    }
                });
            }

        }

        public override IconView? Icon => new()
        {
            Color = "#7B1FA2",
            Name = "icon_videocam"
        };
    }
}
