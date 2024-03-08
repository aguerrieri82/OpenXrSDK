﻿using System.Numerics;

namespace Xr.Engine
{
    public class Object3D : EngineObject, ILayer3DObject
    {
        protected Transform3 _transform;
        protected Group3D? _parent;
        protected bool _worldDirty;
        protected Bounds3 _worldBounds;
        protected bool _boundsDirty;
        protected Scene? _scene;
        protected bool _isVisible;

        private Matrix4x4 _worldMatrixInverse;
        private Matrix4x4 _worldMatrix;
        private bool _worldInverseDirty;

        public Object3D()
        {
            _transform = new Transform3(this);
            _worldDirty = true;
            _boundsDirty = true;
            _worldInverseDirty = true;
            IsVisible = true;
        }

        protected internal virtual void InvalidateWorld()
        {
            _worldDirty = true;
        }

        public virtual T? Feature<T>() where T : class
        {
            if (this is T tInt)
                return tInt;
            return _components?.OfType<T>().FirstOrDefault();
        }

        public virtual bool UpdateWorldMatrix(bool updateChildren, bool updateParent)
        {
            bool isParentChanged = false;
            bool isChanged = false;

            if (updateParent && _parent != null)
                isParentChanged = _parent.UpdateWorldMatrix(false, updateParent);

            if (_transform.Update() || isParentChanged || _worldDirty)
            {
                if (_parent != null && !_parent.WorldMatrix.IsIdentity)
                    _worldMatrix = _transform.Matrix * _parent!.WorldMatrix;
                else
                    _worldMatrix = _transform.Matrix;

                _worldInverseDirty = true;
                _boundsDirty = true;
                _worldDirty = false;

                isChanged = true;
            }

            return isChanged;
        }

        protected void UpdateWorldInverse()
        {
            Matrix4x4.Invert(_worldMatrix, out _worldMatrixInverse);
            //_worldMatrixInverse = MathUtils.InvertRigidBody(_worldMatrix);
            _worldInverseDirty = false;
        }

        //TODO protected
        public virtual void UpdateBounds()
        {
            _boundsDirty = false;
        }

        public override void Update(RenderContext ctx)
        {
            if (_scene == null && _parent != null)
                _scene = this.FindAncestor<Scene>();

            //TODO can we made in update?
            if (_components != null)
            {
                foreach (var component in _components.OfType<IDrawGizmos>())
                    component.DrawGizmos(_scene!.Gizmos);
            }

            base.Update(ctx);
        }

        internal void SetParent(Group3D? value, bool preserveTransform)
        {
            var changeType = ObjectChangeType.Parent | ObjectChangeType.Transform;

            var curWorldMatrix = WorldMatrix;

            _parent = value;

            if (_scene == null && value != null)
                changeType |= ObjectChangeType.SceneAdd;

            _scene = _parent == null ? null : this.FindAncestor<Scene>();

            if (_scene == null)
                changeType |= ObjectChangeType.SceneRemove;

            if (preserveTransform)
                WorldMatrix = curWorldMatrix;

            NotifyChanged(changeType);
        }

        public override void NotifyChanged(ObjectChange change)
        {
            if (change.IsAny(ObjectChangeType.Transform))
                InvalidateWorld();

            _scene?.NotifyChanged(this, change);
        }

        public Group3D? Parent => _parent;

        public Scene? Scene => _scene;

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
            get => new Vector3(0f, 1f, 0f).ToDirection(WorldMatrix);
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
                if (UpdateWorldMatrix(false, false) || _worldInverseDirty)
                    UpdateWorldInverse();
                return _worldMatrixInverse;
            }
        }

        public Matrix4x4 WorldMatrix
        {
            get
            {
                if (_transform.Update() || _worldDirty)
                    UpdateWorldMatrix(false, false);
                return _worldMatrix;
            }
            set
            {
                if (_parent == null || _parent.WorldMatrix.IsIdentity)
                    _transform.SetMatrix(value);
                else
                    _transform.SetMatrix(_parent.WorldMatrixInverse * value);
            }
        }

        public Transform3 Transform => _transform;

        public string? Tag { get; set; }

        public string? Name { get; set; }
    }
}
