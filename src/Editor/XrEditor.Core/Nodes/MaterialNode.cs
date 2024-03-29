using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.Binding;
using XrEngine;


namespace XrEditor.Nodes
{
    public class MaterialNode<T> : EngineObjectNode<T> where T : Material
    {
        public MaterialNode(T value) : base(value)
        {
        }

        public override string DisplayName => _value.Name ?? _value.GetType().Name;

        protected override void EditorProperties(Binder<T> binder, IList<PropertyView> curProps)
        {
            binder.PropertyChanged += (_, _, _, _) => _value.NotifyChanged(ObjectChangeType.Render);

            curProps.Add(new PropertyView
            {
                Label = "Use Depth",
                Editor = new BoolEditor(binder.Prop(a => a.UseDepth))
            });


            curProps.Add(new PropertyView
            {
                Label = "Write Depth",
                Editor = new BoolEditor(binder.Prop(a => a.WriteDepth))
            });

            curProps.Add(new PropertyView
            {
                Label = "Double Sided",
                Editor = new BoolEditor(binder.Prop(a => a.DoubleSided))
            });

            curProps.Add(new PropertyView
            {
                Label = "Write Color",
                Editor = new BoolEditor(binder.Prop(a => a.WriteColor))
            });

            curProps.Add(new PropertyView
            {
                Label = "Alpha",
                Editor = new EnumEditor<AlphaMode>(binder.Prop(a => a.Alpha))
            });

            base.EditorProperties(binder, curProps);
        }

        public override IconView? Icon => new()
        {
            Color = "#689F38",
            Name = "icon_image"
        };
    }
}
