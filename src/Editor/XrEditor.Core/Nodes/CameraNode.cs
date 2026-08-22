using UI.Binding;
using XrEngine;

namespace XrEditor.Nodes
{
    public class CameraNode<T> : Object3DNode<T> where T : Camera
    {
        public CameraNode(T value) : base(value)
        {
            _autoGenProps = PropertiesGenerationMode.None;
        }

        protected override void EditorProperties(Binder<T> binder, IList<PropertyView> curProps)
        {
            // base.EditorProperties(binder, curProps);

            binder.PropertyChanged += async (_, prop, _, _) =>
            {
                await EngineApp.MainThread;

                _value.NotifyChanged(ChangeType.Render);
            };

            curProps.Add(new PropertyView
            {
                Label = "Background Color",
                Editor = new ColorEditor(binder.Prop(a => a.BackgroundColor))
            });

            curProps.Add(new PropertyView
            {
                Label = "Exposure",
                Editor = new FloatEditor(binder.Prop(a => a.Exposure))
            });

            curProps.Add(new PropertyView
            {
                Label = "Far",
                Editor = new FloatEditor(binder.Prop(a => a.Far)),
            });

            curProps.Add(new PropertyView
            {
                Label = "Near",
                Editor = new FloatEditor(binder.Prop(a => a.Near)),
            });

            if (_value is PerspectiveCamera persp)
            {
                curProps.Add(new PropertyView
                {
                    Label = "FovDegree",
                    Editor = new FloatEditor(binder.Prop(a => (a as PerspectiveCamera)!.FovDegree)),
                });

                curProps.Add(new PropertyView
                {
                    Label = "ActiveEye",
                    Editor = new TextEditor<int>(int.Parse, a => a.ToString())
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
