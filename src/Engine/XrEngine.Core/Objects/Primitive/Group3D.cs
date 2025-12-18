using XrMath;

namespace XrEngine
{
    public class Group3D : Object3D, ILocalBounds
    {
        protected List<Object3D> _children = [];
        private Bounds3 _localBounds;

        public Group3D()
        {
            BoundUpdateMode = UpdateMode.Manual;
        }

        public override void Dispose()
        {
            for (var i = _children.Count - 1; i >= 0; i--)
                _children[i].Dispose();

            base.Dispose();
        }

        protected override void OnChanged(ObjectChange change)
        {
            if (change.IsAny(ObjectChangeType.Visibility))
            {
                foreach (var child in this.Descendants())
                    child._visibleDirty = true;
            }
            base.OnChanged(change);
        }

        public override void GetState(IStateContainer container)
        {
            base.GetState(container);

            if ((Flags & EngineObjectFlags.ChildrenGenerated) == EngineObjectFlags.ChildrenGenerated)
                return;

            if ((container.Context.Flags & StateContextFlags.SelfOnly) != 0)
                return;

            container.WriteArray(nameof(Children), _children);
        }

        protected override void SetStateWork(IStateContainer container)
        {
            base.SetStateWork(container);

            if ((Flags & EngineObjectFlags.ChildrenGenerated) == EngineObjectFlags.ChildrenGenerated)
                return;

            if ((container.Context.Flags & StateContextFlags.SelfOnly) != 0)
                return;

            container.ReadArray(nameof(Children), _children, a => AddChild(a), a => RemoveChild(a));
        }

        protected internal override void InvalidateWorld()
        {
            if (_worldDirty)
                return;

            base.InvalidateWorld();

            foreach (var child in _children)
                child.InvalidateWorld();
        }

        public override void Reset(bool onlySelf = false)
        {
            base.Reset(onlySelf);

            if (!onlySelf)
            {
                foreach (var child in _children)
                    child.Reset();
            }
        }

        public override void Update(RenderContext ctx)
        {
            UpdateSelf(ctx);
            if (!ctx.UpdateOnlySelf)
                _children.Update(ctx);
        }

        protected virtual void UpdateSelf(RenderContext ctx)
        {
            base.Update(ctx);
        }

        public T AddChild<T>(T child, bool preserveTransform = false) where T : Object3D
        {
            if (child.Parent == this)
                return child;

            _scene?.EnsureNotLocked();

            if (preserveTransform && child.Parent != null && child.Parent.WorldMatrix == WorldMatrix)
                preserveTransform = false;

            var curWorldMatrix = child.WorldMatrix;

            child.Parent?.RemoveChild(child);

            _children.Add(child);

            child.SetParent(this, preserveTransform);

            if (preserveTransform)
                child.WorldMatrix = curWorldMatrix;

            //child.EnsureId();

            NotifyChanged(new ObjectChange(ObjectChangeType.ChildAdd, child));

            InvalidateBounds();

            return child;
        }

        //TODO: optmize
        public override void UpdateBounds(bool force = false)
        {
            if (!_boundsDirty && !force)
                return;

            var builder = new Bounds3Builder();

            if (_children.Count > 0)
            {
                foreach (var child in _children)
                {
                    if (force)
                        child.UpdateBounds(true);

                    var childLocal = child.Feature<ILocalBounds>();

                    if (childLocal != null)
                    {
                        var childLocalBounds = childLocal.LocalBounds.Transform(child.Transform.Matrix);

                        builder.Add(childLocalBounds);
                    }
                }
            }

            _localBounds = builder.Result;
            _worldBounds = _localBounds.Transform(WorldMatrix);

            base.UpdateBounds();
        }

        public void RemoveChild(Object3D child, bool preserveTransform = false)
        {
            if (child.Parent != this)
                return;

            _scene?.EnsureNotLocked();

            _children.Remove(child);

            child.SetParent(null, preserveTransform);

            NotifyChanged(new ObjectChange(ObjectChangeType.ChildRemove, child));

            InvalidateBounds();
        }

        public int ChildIndex(Object3D object3D)
        {
            return _children.IndexOf(object3D);
        }
        public Bounds3 LocalBounds
        {
            get
            {
                if (_boundsDirty && BoundUpdateMode == UpdateMode.Automatic)
                    UpdateBounds();
                return _localBounds;
            }
        }

        public UpdateMode BoundUpdateMode { get; set; }

        public IReadOnlyList<Object3D> Children => _children.AsReadOnly();



    }
}
