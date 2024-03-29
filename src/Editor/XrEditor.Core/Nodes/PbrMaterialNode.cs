using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.Binding;
using XrEngine;


namespace XrEditor.Nodes
{
    public class PbrMaterialNode : MaterialNode<PbrMaterial>
    {
        public PbrMaterialNode(PbrMaterial value) : base(value)
        {
        }

        protected override void EditorProperties(Binder<PbrMaterial> binder, IList<PropertyView> curProps)
        {


            base.EditorProperties(binder, curProps);

            if (_value.MetallicRoughness != null)
            {
                curProps.Add(new PropertyView
                {
                    Label = "Base Color",
                    Editor = new ColorEditor(binder.Prop(a => a.MetallicRoughness!.BaseColorFactor))
                });

                curProps.Add(new PropertyView
                {
                    Label = "Roughness",
                    Editor = new FloatEditor(binder.Prop(a => a.MetallicRoughness!.RoughnessFactor), 0, 1, 0.01f)
                });

                curProps.Add(new PropertyView
                {
                    Label = "Metallic",
                    Editor = new FloatEditor(binder.Prop(a => a.MetallicRoughness!.MetallicFactor), 0, 1, 0.01f)
                });

            }


            curProps.Add(new PropertyView
            {
                Label = "Occlusion Strength",
                Editor = new FloatEditor(binder.Prop(a => a.OcclusionStrength), 0, 1, 0.01f)
            });

            curProps.Add(new PropertyView
            {
                Label = "Normal Scale",
                Editor = new FloatEditor(binder.Prop(a => a.NormalScale), 0, 1, 0.01f)
            });



        }

    }
}
