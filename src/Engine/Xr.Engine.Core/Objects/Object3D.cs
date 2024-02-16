using System.Numerics;

namespace OpenXr.Engine
{
    public abstract class Object3D : EngineObject, ILayerObject
    {
        protected Transform _transform;
        protected Group? _parent;
        protected Matrix4x4 _worldMatrix;
        protected bool _worldDirty;
        protected Scene? _scene;
        protected bool _isVisible;
        protected Matrix4x4 _worldMatrixInverse;
        protected Bounds3 _worldBounds;

        public Object3D()
        {
            _transform = new Transform(this);
            _worldDirty = true;
            IsVisible = true;
        }

        public virtual bool UpdateWorldMatrix(bool updateChildren, bool updateParent)
        {
            bool isParentChanged = false;
            bool isChanged = false;

            if (updateParent && _parent != null)
                isParentChanged = _parent.UpdateWorldMatrix(false, updateParent);

            if (isParentChanged || _worldDirty)
            {
                if (_parent != null)
                    _worldMatrix = _parent!.WorldMatrix * _transform.Matrix;
                else
                    _worldMatrix = _transform.Matrix;

                Matrix4x4.Invert(_worldMatrix, out _worldMatrixInverse);

                UpdateWorldBounds();

                _worldDirty = false;

                isChanged = true;
            }

            return isChanged;
        }

        public override void Update(RenderContext ctx)
        {
            if (_transform.Update())
            {
                _worldDirty = true;
                UpdateWorldMatrix(true, false);
            }

            base.Update(ctx);
        }

        protected virtual void UpdateWorldBounds()
        {

        }

        internal void SetParent(Group? value)
        {
            var changeType = ObjectChangeType.Parent;

            _parent = value;
            _worldDirty = true;

            if (_scene == null && value != null)
                changeType |= ObjectChangeType.SceneAdd;

            _scene = value == null ? null : this.FindAncestor<Scene>();

            NotifyChanged(changeType);
        }

        public virtual void NotifyChanged(ObjectChange change)
        {
            _scene?.NotifyChanged(this, change);
        }

        public Group? Parent => _parent;

        public bool IsVisible
        {
            get => _isVisible;
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
            get => new Vector3(0f, 0f, 1f).ToDirection(_worldMatrix);
            set
            {
                Transform.Orientation = Forward.RotationTowards(value);
            }
        }

        public Vector3 Up
        {
            get => new Vector3(0f, 1f, 0f).ToDirection(_worldMatrix);
        }

        public Vector3 WorldPosition
        {
            get => Vector3.Zero.Transform(_worldMatrix);
            set => _transform.Position = _parent != null ? value.Transform(_parent.WorldMatrixInverse) : value;
        }

        public Bounds3 WorldBounds => _worldBounds;

        public Scene? Scene => _scene;

        public Matrix4x4 WorldMatrixInverse => _worldMatrixInverse;

        public Matrix4x4 WorldMatrix => _worldMatrix;

        public Transform Transform => _transform;

        public string? Tag { get; set; }

        public string? Name { get; set; }
    }
}
