using UI.Binding;
using XrEngine;

namespace XrEditor.Nodes
{
    public class CameraNode<T> : Object3DNode<T> where T : Camera
    {
        public CameraNode(T value) : base(value)
        {
        }

        protected override void EditorProperties(Binder<T> binder, IList<PropertyView> curProps)
        {
            base.EditorProperties(binder, curProps);

            binder.PropertyChanged += (_, _, _, _) => _value.NotifyChanged(ObjectChangeType.Render);

            curProps.Add(new PropertyView
            {
                Label = "Background Color",
                Editor = new ColorEditor(binder.Prop(a => a.BackgroundColor))
            });


            curProps.Add(new PropertyView
            {
                Label = "Exposure",
                Editor = new FloatEditor(binder.Prop(a => a.Exposure), 0, 5)
            });

            curProps.Add(new PropertyView
            {
                Label = "Far",
                Editor = new FloatEditor(binder.Prop(a => a.Far), new LogScale() { ScaleMin = -3, ScaleMax = 3 }),
            });

            curProps.Add(new PropertyView
            {
                Label = "Near",
                Editor = new FloatEditor(binder.Prop(a => a.Near), new LogScale() { ScaleMin = -3, ScaleMax = 3 }),
            });

        }

        public override IconView? Icon => new()
        {
            Color = "#7B1FA2",
            Name = "icon_videocam"
        };
    }
}
