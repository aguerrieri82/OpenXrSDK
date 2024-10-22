using System.Numerics;
using XrEngine.Interaction;
using XrInteraction;

namespace XrEngine
{
    public class SurfaceController : Behavior<Object3D>, IRayTarget, ISurfaceInput
    {
        protected Dictionary<IRayPointer, RayPointerStatus> _pointerStatus = [];
        Vector2 _pointer;
        bool _pointerValid;
        InputButton _mainInBtn;
        InputButton _secInBtn;
        ICollider3D? _collider;

        public SurfaceController()
        {

        }

        protected override void Start(RenderContext ctx)
        {
            Pointers ??= _host!.Scene?
                .Components<IComponent>()
                .OfType<IRayPointer>()
                .ToArray();

            _collider = _host!.Feature<ICollider3D>();
        }

        protected override void Update(RenderContext ctx)
        {
            ProcessPointers(ctx);
        }

        protected void ProcessPointers(RenderContext ctx)
        {

            if (Pointers == null)
                return;

            bool found = false;

            _mainInBtn.IsChanged = false;
            _secInBtn.IsChanged = false;

            foreach (var pointer in Pointers)
            {

                var status = pointer.GetPointerStatus();

                if (!_pointerStatus.TryGetValue(pointer, out var lastStatus))
                    lastStatus = new RayPointerStatus();

                _pointerStatus[pointer] = status;

                if (!status.IsActive)
                    continue;

                if (_collider is QuadCollider quad)
                    quad.PlaneMode = pointer.IsCaptured;

                var collision = _collider!.CollideWith(status.Ray);

                if (collision != null)
                {
                    NotifyCollision(ctx, collision);
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
                var wasRightDown = (lastStatus.Buttons & Pointer2Button.Left) == Pointer2Button.Left;
                if (rightDown != wasRightDown)
                {
                    _secInBtn.IsChanged = true;
                    _secInBtn.IsDown = rightDown;
                }
            }

            if (!found)
                _pointerValid = false;
        }

        public void NotifyCollision(RenderContext ctx, Collision? collision)
        {
            if (collision != null)
                _pointer = collision.UV ?? new Vector2(collision.LocalPoint.X, collision.LocalPoint.Y) + new Vector2(0.5f, 0.5f);

            _pointerValid = collision != null;
        }

        public bool IsPointerValid => _pointerValid;

        public Vector2 Pointer => _pointer;

        public InputButton MainButton => _mainInBtn;

        public InputButton SecondaryButton => _secInBtn;

        public IList<IRayPointer>? Pointers { get; set; }
    }
}
