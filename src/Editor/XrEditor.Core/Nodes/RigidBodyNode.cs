using PhysX.Framework;
using UI.Binding;
using XrEngine.Physics;

namespace XrEditor.Nodes
{
    public class RigidBodyNode : ComponentNode<RigidBody>
    {
        public RigidBodyNode(RigidBody value) : base(value)
        {
            _autoGenProps = false;
        }


        protected override void EditorProperties(Binder<RigidBody> binder, IList<PropertyView> curProps)
        {
            base.EditorProperties(binder, curProps);

            binder.PropertyChanged += (_, _, _, _) =>
            {
                binder.Value.UpdatePhysics();
            };

            curProps.Add(new PropertyView
            {
                Label = "Type",
                Editor = new EnumEditor<PhysicsActorType>(binder.Prop(a => a.Type))
            });

            curProps.Add(new PropertyView
            {
                Label = "Restitution",
                Editor = new FloatEditor(binder.Prop(a => a.Material.Restitution), 0, 1)
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
                Label = "Auto Teleport",
                Editor = new BoolEditor(binder.Prop(a => a.AutoTeleport))
            });

            if (binder.Value.Type != PhysicsActorType.Static && binder.Value.IsCreated)
            {
                curProps.Add(new PropertyView
                {
                    Label = "Mass",
                    ReadOnly = true,
                    Editor = new FloatEditor(binder.Prop(a => a.DynamicActor.Mass), 0, 1)
                });
            }

            curProps.Add(new PropertyView
            {
                Label = "Angular Damping",
                Category = "Advanced",
                Editor = new FloatEditor(binder.Prop(a => a.AngularDamping), 0, 1)
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

        private void Binder_PropertyChanged(RigidBody? obj, IProperty property, object? value, object? oldValue)
        {
            throw new NotImplementedException();
        }
    }
}
