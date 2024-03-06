using System.Numerics;

namespace Xr.Engine
{
    public class Group3D : Object3D
    {
        protected List<Object3D> _children = [];

        public Group3D()
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

        public override void UpdateWorldBounds()
        {
            var bounds = new Bounds3();

            if (_children.Count > 0)
            {
                bounds = _children[0].WorldBounds;
            
                foreach (var child in _children.Skip(1))
                {
                    bounds.Min = Vector3.Min(child.WorldBounds.Min, bounds.Min);
                    bounds.Max = Vector3.Min(child.WorldBounds.Max, bounds.Max);
                }
            }

            _worldBounds = bounds;
        }

        public void RemoveChild(Object3D child, bool preserveTransform = false)
        {
            if (child.Parent != this)
                return;

            _children.Remove(child);

            child.SetParent(null, preserveTransform);
        }

        public IReadOnlyList<Object3D> Children => _children.AsReadOnly();

    }
}
