using System.Numerics;
using XrMath;

namespace XrEngine
{
    public struct CameraEye
    {
        public Matrix4x4 ViewProj;

        public Matrix4x4 ViewProjInv;

        public Matrix4x4 View;

        public Matrix4x4 World;

        public Matrix4x4 Projection;

    }

    public abstract class Camera : Object3D
    {
        protected Matrix4x4 _projInverse;
        protected Matrix4x4 _proj;
        protected Matrix4x4 _viewProj;
        protected Matrix4x4 _viewProjInverse;
        protected Vector3 _target;
        protected bool _viewProjDirty;
        protected Size2I _viewSize;
        protected bool _projDirty;
        protected bool _projInverseDirty;
        protected float _near;
        protected float _far;

        public Camera()
            : this(true)
        {
        }

        public Camera(bool mustInit)
        {
            if (!mustInit)
                return;
            Near = 0.001f;
            Far = 10;
            Exposure = 1;
            Flags |= EngineObjectFlags.DisableNotifyChangedScene;
        }

        public void LookAt(Vector3 position, Vector3 target, Vector3 up)
        {
            _target = target;
            View = Matrix4x4.CreateLookAt(position, target, up);
        }

        public override void GetState(IStateContainer container)
        {
            base.GetState(container);
            container.Write(nameof(BackgroundColor), BackgroundColor);
            container.Write(nameof(Near), Near);
            container.Write(nameof(Far), Far);
            container.Write(nameof(Exposure), Exposure);
            container.Write(nameof(Projection), Projection);
            container.Write(nameof(Target), Target);
        }

        protected override void SetStateWork(IStateContainer container)
        {
            base.SetStateWork(container);

            BackgroundColor = container.Read<Color>(nameof(BackgroundColor));
            Near = container.Read<float>(nameof(Near));
            Far = container.Read<float>(nameof(Far));
            Exposure = container.Read<float>(nameof(Exposure));
            Projection = container.Read<Matrix4x4>(nameof(Projection));
            _target = container.Read<Vector3>(nameof(Target));

            InvalidateWorld();
        }

        protected internal override void InvalidateWorld()
        {
            _viewProjDirty = true;
            base.InvalidateWorld();
        }

        public Camera Clone()
        {
            var camera = (Camera)Activator.CreateInstance(GetType(), [false])!;
            camera.CopyFrom(this);
            return camera;
        }

        public virtual void CopyFrom(Camera camera)
        {
            _viewProjDirty = true;
            _projInverseDirty = true;

            _near = camera.Near;
            _far = camera.Far;
            _proj = camera.Projection;
            _target = camera.Target;
            _viewSize = camera._viewSize;

            BackgroundColor = camera.BackgroundColor;
            Exposure = camera.Exposure;
            WorldMatrix = camera.WorldMatrix;
            Eyes = camera.Eyes;
            ActiveEye = camera.ActiveEye;
            IsStereo = camera.IsStereo;
            IsMultiView = camera.IsMultiView;
            ViewSize = camera.ViewSize;
        }

        protected virtual void ExtractProjectionInternals()
        {

        }

        protected virtual void BuildProjection()
        {

        }

        protected void UpdateViewProjectionAndInverse()
        {
            _viewProj = View * Projection;
            _viewProjInverse = _viewProj.Invert();
            _viewProjDirty = false;
        }

        protected override void OnChanged(ObjectChange change)
        {
            if (change.IsAny(ChangeType.Transform))
                _viewProjDirty = true;

            base.OnChanged(change);
        }

        public Vector3 Target
        {
            get => _target;
            set
            {
                LookAt(WorldPosition, value, Up);
            }
        }

        public Matrix4x4 View
        {
            get => WorldMatrixInverse;
            set
            {
                var inverse = value.Invert();
                WorldMatrix = inverse;
                _viewProjDirty = true;
            }
        }

        public Matrix4x4 Projection
        {
            get
            {
                if (_projDirty)
                    BuildProjection();
                return _proj;
            }
            set
            {
                if (value == _proj)
                    return;

                _proj = value;
                _projDirty = false;
                _projInverseDirty = true;
                _viewProjDirty = true;
                ExtractProjectionInternals();
            }
        }

        public Matrix4x4 ViewProjection
        {
            get
            {
                if (_viewProjDirty)
                    UpdateViewProjectionAndInverse();
                return _viewProj;
            }
        }

        public Matrix4x4 ViewProjectionInverse
        {
            get
            {
                if (_viewProjDirty)
                    UpdateViewProjectionAndInverse();
                return _viewProjInverse;
            }
        }

        public Matrix4x4 ProjectionInverse
        {
            get
            {
                if (_projInverseDirty)
                {
                    _projInverse = _proj.Invert();
                    _projInverseDirty = false;
                }

                return _projInverse;
            }
        }

        [Range(0.01f, 1f, 0.01f)]
        public float Near
        {
            get => _near;
            set
            {
                if (_near == value)
                    return;
                _near = value;
                _projDirty = true;
                _viewProjDirty = true;
            }
        }

        [Range(0.5f, 1000, 1)]
        public float Far
        {
            get => _far;
            set
            {
                if (_far == value)
                    return;

                _far = value;

                _projDirty = true;
                _viewProjDirty = true;
            }
        }

        public Size2I ViewSize
        {
            get => _viewSize;
            set
            {
                if (_viewSize.Width == value.Width && _viewSize.Height == value.Height)
                    return;

                _viewSize = value;

                _projDirty = true;
                _viewProjDirty = true;
            }
        }

        public Matrix4x4 ViewInverse => WorldMatrix;

        [Range(0, 10, 0.05f)]
        public float Exposure { get; set; }

        public CameraEye[]? Eyes { get; set; }

        public int ActiveEye { get; set; }

        public bool IsStereo { get; set; }

        public bool IsMultiView { get; set; }

        public Color BackgroundColor { get; set; }

    }
}
