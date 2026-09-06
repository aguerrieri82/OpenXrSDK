using UI.Binding;
using XrEngine;
using XrMath;

namespace XrEditor.Nodes
{
    public class Transform3DNode : BaseNode<Transform3D>, IEditorProperties, IItemView, IEditorActions
    {
        public Transform3DNode(Transform3D value, INode parent) : base(value)
        {
            _parent = parent;
        }

        public void EditorProperties(IList<PropertyView> curProps)
        {
            var binder = new Binder<Transform3D>(_value, a => EngineApp.Current.Dispatcher.Post(a));

            curProps.Add(new PropertyView
            {
                Label = "Scale",
                Editor = new Vector3Editor(binder.Prop(a => a.Scale)) { LockedVisible = true }
            });

            curProps.Add(new PropertyView
            {
                Label = "Position",
                Editor = new Vector3Editor(binder.Prop(a => a.Position))
            });

            curProps.Add(new PropertyView
            {
                Label = "Rotation",
                Editor = new Vector3Editor(binder.Prop(a => a.Rotation))
            });

            curProps.Add(new PropertyView
            {
                Label = "Local Pivot",
                Editor = new Vector3Editor(binder.Prop(a => a.LocalPivot))
            });
        }

        public void EditorActions(IList<ActionView> curActions)
        {
            curActions.Add(new ActionView
            {
                DisplayName = "Copy Pose",
                IsActive = true,
                IsEnabled = true,
                Name = "copy-pose",
                ExecuteCommand = new Command(() =>
                {
                    var pose = _value.ToPose();
                    var clip = Context.Require<IClipboard>();
                    var code = FormattableString.Invariant($"new Pose3()\n{{\n    Position = new Vector3({pose.Position.X}f, {pose.Position.Y}f, {pose.Position.Z}f),\n    Orientation=new Quaternion({pose.Orientation.X}f,{pose.Orientation.Y}f,{pose.Orientation.Z}f,{pose.Orientation.W}f)\n}};");
                    clip.Copy(code, "text/plain");
                })
            });
        }

        public string DisplayName => "Transform";

        public IconView? Icon => null;

        PropertiesGenerationMode IEditorProperties.AutoGenerate { get; set; }
    }
}
