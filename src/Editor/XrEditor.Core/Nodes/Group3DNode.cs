using XrEditor.Services;
using XrEngine;

namespace XrEditor.Nodes
{
    public class Group3DNode<T> : Object3DNode<T>, IDynamicNode where T : Group3D
    {
        public Group3DNode(T value)
            : base(value)
        {

        }

        protected override void OnElementChanged(EngineObject element, ObjectChange change)
        {
            if ((change.Type & ObjectChangeType.ChildAdd) == ObjectChangeType.ChildAdd)
                ChildAdded?.Invoke(this, Context.Require<NodeManager>().CreateNode(change.Target!));
            if ((change.Type & ObjectChangeType.ChildRemove) == ObjectChangeType.ChildRemove)
                ChildRemoved?.Invoke(this, Context.Require<NodeManager>().CreateNode(change.Target!));
            base.OnElementChanged(element, change);
        }

        public override IEnumerable<INode> Children
        {
            get
            {
                var factory = Context.Require<NodeManager>();
                return _value.Children.Select(a => factory.CreateNode(a));
            }
        }


        public event ChildNodeDelegate? ChildAdded;

        public event ChildNodeDelegate? ChildRemoved;

        public override IconView? Icon => new()
        {
            Color = "#1565C0",
            Name = "icon_category"
        };
    }
}
