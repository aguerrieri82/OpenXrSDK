using System.Numerics;
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

        public override void GetState(IStateContainer container)
        {
            base.GetState(container);

            if ((Flags & EngineObjectFlags.ChildGenerated) == EngineObjectFlags.ChildGenerated)
                return;

            if ((container.Context.Flags & StateContextFlags.SelfOnly) != 0)
                return;

            container.WriteArray(nameof(Children), _children);
        }

        protected override void SetStateWork(IStateContainer container)
        {
            base.SetStateWork(container);

            if ((Flags & EngineObjectFlags.ChildGenerated) == EngineObjectFlags.ChildGenerated)
                return;

            if ((container.Context.Flags & StateContextFlags.SelfOnly) != 0)
                return;

            container.ReadArray(nameof(Children), _children, a => AddChild(a), a => RemoveChild(a));
        }

        protected internal override void InvalidateWorld()
        {
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

            child.Parent?.RemoveChild(child);

            child.EnsureId();

            child.SetParent(this, preserveTransform);

            _children.Add(child);

            NotifyChanged(new ObjectChange(ObjectChangeType.ChildAdd, child));

            InvalidateBounds();

            return child;
        }

        //TODO: optmize
        public override void UpdateBounds(bool force = false)
        {
            if (!_boundsDirty && !force)
                return;

            var bounds = new Bounds3();

            if (_children.Count > 0)
            {
                bounds.Min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
                bounds.Max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

                foreach (var child in _children)
                {
                    var childLocal = child.Feature<ILocalBounds>();

                    if (childLocal != null)
                    {
                        var childLocalBounds = childLocal.LocalBounds.Transform(child.Transform.Matrix);

                        bounds.Min = Vector3.Min(childLocalBounds.Min, bounds.Min);
                        bounds.Max = Vector3.Max(childLocalBounds.Max, bounds.Max);
                    }
                }
            }

            _localBounds = bounds;
            _worldBounds = bounds.Transform(WorldMatrix);

            base.UpdateBounds();
        }

        public void RemoveChild(Object3D child, bool preserveTransform = false)
        {
            if (child.Parent != this)
                return;

            _children.Remove(child);

            child.SetParent(null, preserveTransform);

            NotifyChanged(new ObjectChange(ObjectChangeType.ChildRemove, child));

            InvalidateBounds();
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
