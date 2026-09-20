using System.Diagnostics;
using System.Numerics;
using XrEngine.Interaction;
using XrInteraction;
using XrMath;

namespace XrEngine
{
    public class SurfaceController : Behavior<Object3D>, IRayTarget, ISurfaceInput, IDrawGizmos
    {
        protected struct TouchPointerState
        {
            public bool IsDown;
            public Vector2 Position;

            public Vector3 LocalPoint;
        }

        protected Dictionary<IRayPointer, RayPointerStatus> _rayPointerStatus = [];
        protected Dictionary<ITouchPointer, TouchPointerState> _touchPointerStatus = [];

        Vector2 _position;
        bool _pointerValid;
        InputButton _mainInBtn;
        InputButton _secInBtn;
        ICollider3D? _collider;
        IPointer? _pointer;

        public SurfaceController()
        {
            UpdatePriority = -1;
            TouchOffset = 0.01f;
            TouchReleaseOffset = 0.02f;
        }

        protected override void Start(RenderContext ctx)
        {
            RayPointers ??= _host.Scene?
                .Components<IComponent>()
                .OfType<IRayPointer>()
                .ToArray();

            TouchPointers ??= _host.Scene?
                .DescendantsOrSelfComponents<IComponent>()
                .OfType<ITouchPointer>()
                .ToArray();

            _collider = _host.Feature<ICollider3D>();
        }

        protected override void Update(RenderContext ctx)
        {
            _pointerValid = false;
            _mainInBtn.IsChanged = false;
            _secInBtn.IsChanged = false;

            ProcessRayPointers(ctx);

            ProcessTouchPointers(ctx);
        }

        protected void ProcessTouchPointers(RenderContext ctx)
        {
            if (TouchPointers == null)
                return;

            foreach (var pointer in TouchPointers)
            {
                var status = pointer.GetPointerStatus();

                if (!status.IsActive) 
                    continue;

                if (!_touchPointerStatus.TryGetValue(pointer, out var state))
                    state = new TouchPointerState();

                var collision = GetTouchCollision(status.Pose.Position);

                var isDown = state.IsDown
                    ? collision != null && collision.Distance >= -TouchReleaseOffset && collision.Distance <= TouchOffset
                    : collision != null && collision.Distance >= 0 && collision.Distance <= TouchOffset;

                var changed = isDown != state.IsDown;

                if (!isDown && !changed)
                    continue;

                _pointer = pointer;

                NotifyCollision(ctx, collision);

                if (isDown)
                {
                    state.Position = _position;
                    state.LocalPoint = collision!.LocalPoint;
                }

                if (changed)
                {
                    Debug.WriteLine(_position.ToString() + " " + isDown);

                    _mainInBtn.IsChanged = true;
                    _mainInBtn.IsDown = isDown;

                    state.IsDown = isDown;
                }

                _touchPointerStatus[pointer] = state;
            }
        }

        protected Collision? GetTouchCollision(Vector3 point)
        {
            switch (_collider)
            {
                case QuadCollider quad:
                    return GetTouchCollision(quad, point);

                default:
                    return null;
            }
        }

        protected Collision? GetTouchCollision(QuadCollider collider, Vector3 point)
        {
            var localPoint = Vector3.Transform(point, _host.WorldMatrixInverse);

            if (localPoint.X < -0.5f || localPoint.X > 0.5f ||
                localPoint.Y < -0.5f || localPoint.Y > 0.5f)
                return null;

            var localSurfacePoint = new Vector3(localPoint.X, localPoint.Y, 0);
            var worldSurfacePoint = Vector3.Transform(localSurfacePoint, _host.WorldMatrix);

            var distance = Vector3.Distance(point, worldSurfacePoint);

            if (localPoint.Z < 0)
                distance = -distance;

            return new Collision
            {
                LocalPoint = localSurfacePoint,
                Distance = distance,
                UV = new Vector2(localPoint.X + 0.5f, localPoint.Y + 0.5f)
            };
        }

        protected void ProcessRayPointers(RenderContext ctx)
        {
            if (RayPointers == null)
                return;

            var found = false;

            foreach (var pointer in RayPointers)
            {
                var status = pointer.GetPointerStatus();

                if (!_rayPointerStatus.TryGetValue(pointer, out var lastStatus))
                    lastStatus = new RayPointerStatus();

                _rayPointerStatus[pointer] = status;

                if (!status.IsActive)
                    continue;

                if (_collider is QuadCollider quad)
                    quad.PlaneMode = pointer.IsCaptured;

                var collision = _collider!.CollideWith(status.Ray);

                if (collision != null)
                {
                    NotifyCollision(ctx, collision);
                    _pointer = pointer;
                    found = true;
                }

                var leftDown = (status.Buttons & Pointer2Button.Left) == Pointer2Button.Left;
                var wasLeftDown = (lastStatus.Buttons & Pointer2Button.Left) == Pointer2Button.Left;

                if (leftDown != wasLeftDown)
                {
                    _mainInBtn.IsChanged = true;
                    _mainInBtn.IsDown = leftDown;

                    if (leftDown)
                    {
                        if (collision != null)
                            pointer.CapturePointer();
                    }
                    else
                    {
                        if (pointer.IsCaptured)
                            pointer.ReleasePointer();
                    }
                }

                var rightDown = (status.Buttons & Pointer2Button.Right) == Pointer2Button.Right;
                var wasRightDown = (lastStatus.Buttons & Pointer2Button.Right) == Pointer2Button.Right;

                if (rightDown != wasRightDown)
                {
                    _secInBtn.IsChanged = true;
                    _secInBtn.IsDown = rightDown;
                }
            }

            if (!found && _pointer is IRayPointer)
                _pointerValid = false;
        }

        public void NotifyCollision(RenderContext ctx, Collision? collision)
        {
            if (collision != null)
                _position = collision.UV ?? new Vector2(collision.LocalPoint.X, collision.LocalPoint.Y) + new Vector2(0.5f, 0.5f);

            _pointerValid = collision != null;
        }


        public void DrawGizmos(Canvas3D canvas, RenderContext ctx)
        {
            if (_pointer is not ITouchPointer touchPointer)
                return;

            if (!_touchPointerStatus.TryGetValue(touchPointer, out var state) || !state.IsDown)
                return;

            var worldPoint = Vector3.Transform(state.LocalPoint, _host.WorldMatrix);
            var worldNormal = Vector3.Normalize(Vector3.TransformNormal(Vector3.UnitZ, _host.WorldMatrix));

            canvas.DrawCircle(new Pose3
            {
                Position = worldPoint,
                Orientation = worldNormal.ToOrientation()
            }, 0.01f);
        }

        public IPointer? Pointer => _pointer;

        public bool IsPointerValid => _pointerValid;

        public Vector2 Position => _position;

        public InputButton MainButton => _mainInBtn;

        public InputButton SecondaryButton => _secInBtn;

        public IList<IRayPointer>? RayPointers { get; set; }

        public IList<ITouchPointer>? TouchPointers { get; set; }

        [Range(0, 0.2f, 0.001f)]
        public float TouchOffset { get; set; }

        [Range(0, 0.2f, 0.001f)]
        public float TouchReleaseOffset { get; set; }
    }
}