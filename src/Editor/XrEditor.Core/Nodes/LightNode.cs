using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.Binding;
using XrEngine;

namespace XrEditor.Nodes
{
    public class LightNode<T> : Object3DNode<T> where T : Light
    {
        public LightNode(T value) : base(value)
        {
        }

        public override IconView? Icon => new()
        {
            Color = "#FBC02D",
            Name = "icon_lightbulb"
        };

        protected override void EditorProperties(Binder<T> binder, IList<PropertyView> curProps)
        {
            base.EditorProperties(binder, curProps);

            binder.PropertyChanged += (_, _, _, _) => _value.NotifyChanged(ObjectChangeType.Render);

            curProps.Add(new PropertyView
            {
                Label = "Intensity",
                Editor = new FloatEditor(binder.Prop(a => a.Intensity), 0, 10, 0.1f)
            });


            curProps.Add(new PropertyView
            {
                Label = "Cast Shadows",
                Editor = new BoolEditor(binder.Prop(a => a.CastShadows))
            });

            curProps.Add(new PropertyView
            {
                Label = "Color",
                Editor = new ColorEditor(binder.Prop(a => a.Color))
            });

        }
    }
}
