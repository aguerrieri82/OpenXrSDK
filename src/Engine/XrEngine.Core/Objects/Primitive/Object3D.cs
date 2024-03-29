﻿using System.Numerics;
using XrMath;

namespace XrEngine
{
    public class Object3D : EngineObject, ILayer3DItem, IStateManager
    {
        protected Bounds3 _worldBounds;
        private Matrix4x4 _worldMatrixInverse;
        private Matrix4x4 _worldMatrix;

        protected bool _worldDirty;
        protected bool _worldInverseDirty;
        protected bool _boundsDirty;

        protected Transform3D _transform;
        protected Group3D? _parent;
        protected Scene3D? _scene;
        protected bool _isVisible;

        private double _creationTime;
        private double _lastUpdateTime;

        public Object3D()
        {
            _transform = new Transform3D(this);
            _worldDirty = true;
            _boundsDirty = true;
            _worldInverseDirty = true;
            _creationTime = -1;
            IsVisible = true;
        }

        public virtual T? Feature<T>() where T : class
        {
            if (this is T tInt)
                return tInt;
            return _components?.OfType<T>().FirstOrDefault();
        }

        public virtual void UpdateWorldMatrix(bool force = false)
        {
            if (!_worldDirty && !force)
                return;

            if (_parent != null && !_parent.WorldMatrix.IsIdentity)
                _worldMatrix = _transform.Matrix * _parent!.WorldMatrix;
            else
                _worldMatrix = _transform.Matrix;

            _worldInverseDirty = true;
            _worldDirty = false;
        }

        public virtual void UpdateBounds(bool force = false)
        {
            _boundsDirty = false;
        }

        protected virtual void Start(RenderContext ctx)
        {

        }

        public override void Update(RenderContext ctx)
        {
            if (_creationTime == -1)
            {
                _creationTime = ctx.Time;
                Start(ctx);
            }

            _lastUpdateTime = ctx.Time;

            if (_scene == null && _parent != null)
                _scene = this.FindAncestor<Scene3D>();

            //TODO can we made in update?
            if (_components != null)
            {
                foreach (var component in _components.OfType<IDrawGizmos>())
                    component.DrawGizmos(_scene!.Gizmos);
            }

            base.Update(ctx);
        }

        protected override void OnChanged(ObjectChange change)
        {
            if (change.IsAny(ObjectChangeType.Transform))
            {
                InvalidateWorld();

                if (this is ILocalBounds local && local.BoundUpdateMode != UpdateMode.Manual)
                    _parent?.InvalidateBounds();
            }

            if (change.IsAny(ObjectChangeType.Geometry))
                InvalidateBounds();

            _scene?.NotifyChanged(this, change);

            base.OnChanged(change);
        }

        public override void Dispose()
        {
            _parent?.RemoveChild(this);
            base.Dispose();
        }

        protected internal virtual void InvalidateWorld()
        {
            _worldDirty = true;
            _worldInverseDirty = true;
        }

        protected internal void InvalidateBounds()
        {
            _parent?.InvalidateBounds();
            _boundsDirty = true;
        }

        protected void UpdateWorldInverse()
        {
            UpdateWorldMatrix();
            Matrix4x4.Invert(_worldMatrix, out _worldMatrixInverse);
            _worldInverseDirty = false;
        }

        internal void SetParent(Group3D? value, bool preserveTransform)
        {
            var changeType = ObjectChangeType.Parent | ObjectChangeType.Transform;

            var curWorldMatrix = WorldMatrix;

            _parent = value;

            if (_scene == null && value != null)
                changeType |= ObjectChangeType.SceneAdd;

            _scene = _parent == null ? null : this.FindAncestor<Scene3D>();

            if (_scene == null)
                changeType |= ObjectChangeType.SceneRemove;

            if (preserveTransform)
                WorldMatrix = curWorldMatrix;

            NotifyChanged(changeType);
        }

        public override void GetState(StateContext ctx, IStateContainer container)
        {
            base.GetState(ctx, container);
            container.Write(nameof(Name), Name);
            container.Write(nameof(Tag), Tag);
            _transform.GetState(ctx, container.Enter("Transform"));
        }


        protected override void SetStateWork(StateContext ctx, IStateContainer container)
        {
            base.SetStateWork(ctx, container);  
            Name = container.Read<string?>(nameof(Name));
            Tag = container.Read<string?>(nameof(Tag));
            _transform.SetState(ctx, container.Enter("Transform"));
        }

        public bool IsVisible
        {
            get => _isVisible && (_parent == null || _parent.IsVisible);
            set
            {
                if (_isVisible == value)
                    return;
                _isVisible = value;
                NotifyChanged(ObjectChangeType.Visibility);
            }
        }

        public Vector3 Forward
        {
            get => new Vector3(0f, 0f, -1f).ToDirection(WorldMatrix);
            set
            {
                Transform.Orientation = Forward.RotationTowards(value);
            }
        }

        public Vector3 Up
        {
            get => Vector3.UnitY.ToDirection(WorldMatrix);
        }

        public Vector3 WorldPosition
        {
            get => _parent != null ?
                    _transform.Position.Transform(_parent.WorldMatrix) : _transform.Position;
            set
            {
                _transform.Position = _parent != null ?
                    value.Transform(_parent.WorldMatrixInverse) : value;
            }
        }

        public Bounds3 WorldBounds
        {
            get
            {
                if (_boundsDirty)
                    UpdateBounds();
                return _worldBounds;
            }
        }

        public Matrix4x4 WorldMatrixInverse
        {
            get
            {
                if (_worldInverseDirty)
                    UpdateWorldInverse();
                return _worldMatrixInverse;
            }
        }

        public Matrix4x4 WorldMatrix
        {
            get
            {
                if (_worldDirty)
                    UpdateWorldMatrix();
                return _worldMatrix;
            }
            set
            {
                if (_parent == null || _parent.WorldMatrix.IsIdentity)
                    _transform.SetMatrix(value);
                else
                    _transform.SetMatrix(_parent.WorldMatrixInverse * value);

                _worldInverseDirty = true;
            }
        }

        public Group3D? Parent => _parent;

        public Scene3D? Scene => _scene;

        public double CreationTime => _creationTime;

        public double LifeTime => _lastUpdateTime - _creationTime;

        public Transform3D Transform => _transform;

        public string? Tag { get; set; }

        public string? Name { get; set; }
    }
}