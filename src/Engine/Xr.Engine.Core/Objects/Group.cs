namespace OpenXr.Engine
{
    public class Group : Object3D
    {
        protected List<Object3D> _children = [];

        public Group()
        {
        }

        protected internal override void InvalidateWorld()
        {
            foreach (var child in _children)
                child.InvalidateWorld();
            base.InvalidateWorld();
        }

        public override bool UpdateWorldMatrix(bool updateChildren, bool updateParent)
        {
            var isChanged = base.UpdateWorldMatrix(updateChildren, updateParent);

            if (updateChildren || isChanged)
                _children.ForEach(a => a.UpdateWorldMatrix(true, false));

            return isChanged;
        }

        public override void Update(RenderContext ctx)
        {
            base.Update(ctx);

  
            UpdateSelf(ctx);

            _children.Update(ctx);
        }

        protected virtual void UpdateSelf(RenderContext ctx)
        {
            _transform.Update();

        }

        public T AddChild<T>(T child, bool preserveTransform = false) where T : Object3D
        {
            if (child.Parent == this)
                return child;

            child.Parent?.RemoveChild(child);

            child.EnsureId();

            child.SetParent(this, preserveTransform);

            _children.Add(child);

            return child;
        }

        public void RemoveChild(Object3D child, bool preserveTransform = false)
        {
            if (child.Parent != this)
                return;

            _children.Remove(child);

            child.SetParent(null, preserveTransform);
        }

        public IReadOnlyList<Object3D> Children => _children.AsReadOnly();

        public int Version { get; set; }
    }
}
