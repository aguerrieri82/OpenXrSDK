using System.Numerics;
using XrMath;

namespace XrEngine
{
    public class Object3D : EngineObject, ILayer3DItem, IStateManager
    {
        protected Bounds3 _worldBounds;
        private Matrix4x4 _worldMatrixInverse;
        private Matrix4x4 _worldMatrix;
        private Matrix4x4 _normalMatrix;
        protected Vector3[]? _worldPoints;

        protected bool _worldDirty;
        protected bool _worldInverseDirty;
        protected bool _boundsDirty;
        protected bool _normalMatrixDirty;

        protected Transform3D _transform;
        protected Group3D? _parent;
        protected internal Scene3D? _scene;
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
            _normalMatrixDirty = true;
            _worldDirty = false;

            OnWorldChanged();

            if (IsNotifyChangedScene())
                _scene?.NotifyChanged(this, ObjectChangeType.Transform);
        }

        protected virtual void OnWorldChanged()
        {
            _transform.Version++;
        }

        bool IsNotifyChangedScene()
        {
            var curItem = this;

            while (curItem != null)
            {
                if ((curItem.Flags & EngineObjectFlags.NotifyChangedScene) != 0)
                    return true;

                if ((curItem.Flags & EngineObjectFlags.DisableNotifyChangedScene) != 0)
                    return false;

                curItem = curItem.Parent;
            }

            return true;
        }

        public virtual void UpdateBounds(bool force = false)
        {
            _boundsDirty = false;
        }

        protected virtual void Start(RenderContext ctx)
        {

        }

        public override void Reset(bool onlySelf = false)
        {
            _creationTime = -1;
            base.Reset(onlySelf);
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

            //Transform changes are notified when world matrix is updated
            if (_parent != null && change.Type != ObjectChangeType.Transform)
                _scene?.NotifyChanged(this, change);

            base.OnChanged(change);
        }

        public override void Dispose()
        {
            _parent?.RemoveChild(this);
            base.Dispose();
        }

        public float DistanceTo(Vector3 point)
        {
            _worldPoints ??= WorldBounds.Points.ToArray();
            return _worldPoints.MinDistanceTo(point);
        }

        protected internal virtual void InvalidateWorld()
        {
            _worldDirty = true;
            _worldInverseDirty = true;
            _boundsDirty = true;
            _normalMatrixDirty = true;
        }

        protected internal void InvalidateBounds()
        {
            _parent?.InvalidateBounds();
            _boundsDirty = true;
            _worldPoints = null;
        }

        protected void UpdateWorldInverse()
        {
            Matrix4x4.Invert(WorldMatrix, out _worldMatrixInverse);
            _worldInverseDirty = false;
        }

        protected void UpdateNormalMatrix()
        {
            _normalMatrix = Matrix4x4.Transpose(WorldMatrixInverse);
            _normalMatrixDirty = false;
        }

        internal void SetParent(Group3D? value, bool preserveTransform)
        {
            var changeType = ObjectChangeType.Parent | ObjectChangeType.Transform;

            var curWorldMatrix = WorldMatrix;

            _parent = value;

            if (_scene == null && value != null)
                changeType |= ObjectChangeType.SceneAdd;

            var oldScene = _scene;

            _scene = _parent == null ? null : this.FindAncestor<Scene3D>();

            if (_scene == null)
            {
                changeType |= ObjectChangeType.SceneRemove;
                oldScene?.NotifyChanged(this, changeType);
            }

            if (preserveTransform)
                WorldMatrix = curWorldMatrix;

            NotifyChanged(changeType);
        }

        public override T AddComponent<T>(T component)
        {
            _scene?.EnsureNotLocked();
            return base.AddComponent(component);
        }

        public override void RemoveComponent(IComponent component)
        {
            _scene?.EnsureNotLocked();
            base.RemoveComponent(component);
        }

        public override void GetState(IStateContainer container)
        {
            base.GetState(container);
            container.Write(nameof(Name), Name);
            container.Write(nameof(Tag), Tag);
            container.Write(nameof(IsVisible), IsVisible);
            _transform.GetState(container.Enter("Transform"));
        }

        protected override void SetStateWork(IStateContainer container)
        {
            base.SetStateWork(container);

            Name = container.Read<string?>(nameof(Name));
            Tag = container.Read<string?>(nameof(Tag));
            IsVisible = container.Read<bool>(nameof(IsVisible));
            _transform.SetState(container.Enter("Transform"));
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
            get => -Vector3.UnitZ.ToDirection(WorldMatrix);
            set
            {
                WorldOrientation = -value.ToOrientation();
            }
        }

        public Vector3 Up
        {
            get => Vector3.UnitY.ToDirection(WorldMatrix);
        }

        public Vector3 WorldPosition
        {
            get => _parent != null && !_parent.WorldMatrix.IsIdentity ?
                   _transform.Position.Transform(_parent.WorldMatrix) : _transform.Position;
            set
            {
                _transform.Position = _parent != null && !_parent.WorldMatrix.IsIdentity ?
                    value.Transform(_parent.WorldMatrixInverse) : value;
            }
        }

        public Quaternion WorldOrientation
        {
            get => _parent != null && !_parent.WorldMatrix.IsIdentity ?
                   _parent.WorldOrientation * _transform.Orientation :
                   _transform.Orientation;
            set
            {
                _transform.Orientation = _parent != null && !_parent.WorldMatrix.IsIdentity ?
                    Quaternion.Conjugate(_parent.WorldOrientation) * value :
                    value;
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
                _normalMatrixDirty = true;
            }
        }

        public Matrix4x4 NormalMatrix
        {
            get
            {
                if (_normalMatrixDirty)
                    UpdateNormalMatrix();
                return _normalMatrix;
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
