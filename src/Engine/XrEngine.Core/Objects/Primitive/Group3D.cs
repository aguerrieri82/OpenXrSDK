﻿using System.Numerics;
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

        public override void GetState(StateContext ctx, IStateContainer container)
        {
            base.GetState(ctx, container);

            var children = container.Enter(nameof(Children));

            for (var i = 0; i < _children.Count; i++)
                children.WriteTypeObject(ctx, i.ToString(), _children[i]); 
        }

        protected override void SetStateWork(StateContext ctx, IStateContainer container)
        {
            base.SetStateWork(ctx, container);

            var childrenState = container.Enter(nameof(Children));
            HashSet<Object3D> foundChildren = [];
            foreach (var key in childrenState.Keys)
            {
                var childState = childrenState.Enter(key);
                var childId = childState.Read<ObjectId>("Id");

                var curChild = _children.FirstOrDefault(a => a.Id == childId);
                if (curChild == null)
                {
                    var typeName = childState.Read<string>("$type");
                    curChild = (Object3D)ObjectFactory.Instance.CreateObject(typeName);
                    AddChild(curChild);
                }
                else
                    foundChildren.Add(curChild);

                curChild.SetState(ctx, childState);
            }

            for (var i = _children.Count; i >= 0; i--)
            {
                if (!foundChildren.Contains(_children[i]))
                    RemoveChild(_children[i]);
            }

        }

        protected internal override void InvalidateWorld()
        {
            _worldDirty = true;

            foreach (var child in _children)
                child.InvalidateWorld();
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

            InvalidateBounds();

            return child;
        }

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