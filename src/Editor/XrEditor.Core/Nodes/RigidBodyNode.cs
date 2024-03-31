using PhysX.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.Binding;
using XrEngine.Physics;

namespace XrEditor.Nodes
{
    public class RigidBodyNode : ComponentNode<RigidBody>
    {
        public RigidBodyNode(RigidBody value) : base(value)
        {
        }


        protected override void EditorProperties(Binder<RigidBody> binder, IList<PropertyView> curProps)
        {
            base.EditorProperties(binder, curProps);


            curProps.Add(new PropertyView
            {
                Label = "Type",
                Editor = new EnumEditor<PhysicsActorType>(binder.Prop(a => a.Type))
            });

            curProps.Add(new PropertyView
            {
                Label = "Restitution",
                Editor = new FloatEditor(binder.Prop(a => a.Material.Restitution),0, 1)
            });
            
            curProps.Add(new PropertyView
            {
                Label = "Static Friction",
                Editor = new FloatEditor(binder.Prop(a => a.Material.StaticFriction), 0, 1)
            });

            curProps.Add(new PropertyView
            {
                Label = "Dynamic Friction",
                Editor = new FloatEditor(binder.Prop(a => a.Material.StaticFriction), 0, 1)
            });

            curProps.Add(new PropertyView
            {
                Label = "Density",
                Editor = new FloatEditor(binder.Prop(a => a.Density), 0, 1)
            });

            curProps.Add(new PropertyView
            {
                Label = "Contact Offset",
                Category = "Advanced",
                Editor = new FloatEditor(binder.Prop(a => a.ContactOffset), 0, 1)
            });

            curProps.Add(new PropertyView
            {
                Label = "Contact Report Threshold",
                Category = "Advanced",
                Editor = new FloatEditor(binder.Prop(a => a.ContactReportThreshold), 0, 1)
            });

            curProps.Add(new PropertyView
            {
                Label = "Enable CCD",
                Category = "Advanced",
                Editor = new BoolEditor(binder.Prop(a => a.EnableCCD))
            });

            curProps.Add(new PropertyView
            {
                Label = "Length Tolerance Scale",
                Category = "Advanced",
                Editor = new FloatEditor(binder.Prop(a => a.LengthToleranceScale), 0, 1000)
            });


        }

    }
}
