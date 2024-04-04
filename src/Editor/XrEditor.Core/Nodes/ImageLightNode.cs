using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.Binding;
using XrEngine;

namespace XrEditor.Nodes
{
    public class ImageLightNode : LightNode<ImageLight>
    {
        public ImageLightNode(ImageLight value) : base(value)
        {
            _autoGenProps = false;
        }


        protected override void EditorProperties(Binder<ImageLight> binder, IList<PropertyView> curProps)
        {
            base.EditorProperties(binder, curProps);
            
            curProps.Add(new PropertyView
            {
                Label = "Rotation",
                Editor = new FloatEditor(binder.Prop(a => a.Rotation), RadDegreeScale.Instance)
            });

        }
    }
}
