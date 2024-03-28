using Silk.NET.Maths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.Binding;
using XrEngine;

namespace XrEditor.Nodes
{
    public class Transform3DNode : BaseNode<Transform3D>, IEditorProperties, IItemView
    {
        public Transform3DNode(Transform3D value) : base(value)
        {
        }

        public void EditorProperties(IList<PropertyView> curProps)
        {
            var binder = new Binder<Transform3D>(_value);

            curProps.Add(new PropertyView
            {
                Label = "Scale",
                Editor = new Vector3Editor(binder.Prop(a => a.Scale), new ValueScale { ScaleMin = 0.001f, ScaleMax = 10, ScaleStep = 0.1f, ScaleSmallStep = 0.1f })
            });

            curProps.Add(new PropertyView
            {
                Label = "Position",
                Editor = new Vector3Editor(binder.Prop(a => a.Position), new ValueScale { ScaleMin = 0.001f, ScaleMax = 10, ScaleStep = 0.01f, ScaleSmallStep = 0.01f })
            });

            curProps.Add(new PropertyView
            {
                Label = "Rotation",
                Editor = new Vector3Editor(binder.Prop(a => a.Rotation), RadDegreeScale.Instance)
            });
        }

        public string DisplayName => "Transform";

        public IconView? Icon => null;

    }
}
