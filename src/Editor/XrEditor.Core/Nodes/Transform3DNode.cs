using System.Numerics;
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
            var binder = new Binder<Transform3D>(_value);

            curProps.Add(new PropertyView
            {
                Label = "Scale",
                Editor = new Vector3Editor(binder.Prop(a => a.Scale), new ValueScale { ScaleMin = 0.001f, ScaleMax = 10, ScaleStep = 0.1f, ScaleSmallStep = 0.1f }) { LockedVisible = true }
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

            curProps.Add(new PropertyView
            {
                Label = "Local Pivot",
                Editor = new Vector3Editor(binder.Prop(a => a.LocalPivot), new ValueScale { ScaleMin = 0.001f, ScaleMax = 10, ScaleStep = 0.01f, ScaleSmallStep = 0.01f })
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

        bool IEditorProperties.AutoGenerate { get; set; }
    }
}
