using System.Numerics;
using XrEngine.Interaction;
using XrInteraction;

namespace XrEngine
{ 
    public class SurfaceController : Behavior<Object3D>, IRayTarget, ISurfaceInput
    {
        double _pointerTime;
        Vector2 _pointer;
        bool _pointerValid;
        InputButton _mainInBtn;
        InputButton _backInBtn;
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

            _collider = _host!.Component<ICollider3D>();

            base.Start(ctx);
        }

        protected override void Update(RenderContext ctx)
        {
            ProcessPointers(ctx);

            base.Update(ctx);
        }

        protected void ProcessPointers(RenderContext ctx)
        {
       
            if (Pointers == null)
                return;

            bool found = false; 

            foreach (var pointer in Pointers)
            {
                var status = pointer.GetPointerStatus();

                if (!status.IsActive)
                    continue;

                var collision = _collider!.CollideWith(status.Ray);
                if (collision != null)
                {
                    NotifyCollision(ctx, collision);
                    found = true;
                }
      

                var leftDown = (status.Buttons & Pointer2Button.Left) == Pointer2Button.Left;
                if (leftDown != _mainInBtn.IsDown)
                {
                    _mainInBtn.IsChanged = true;
                    _mainInBtn.IsDown = leftDown;
                    if (leftDown && collision != null)
                        pointer.CapturePointer();
                    else
                        pointer.ReleasePointer();
                }
                else
                    _mainInBtn.IsChanged = false;

                var rightDown = (status.Buttons & Pointer2Button.Right) == Pointer2Button.Right;
                if (rightDown != _backInBtn.IsDown)
                {
                    _backInBtn.IsChanged = true;
                    _backInBtn.IsDown = rightDown;
                }
                else
                    _backInBtn.IsChanged = false;

            }

            if (!found)
                _pointerValid = false;
        }

        public void NotifyCollision(RenderContext ctx, Collision collision)
        {
            _pointerTime = ctx.Time;
            _pointer = new Vector2(collision.LocalPoint.X, collision.LocalPoint.Y) + new Vector2(0.5f, 0.5f);
            Console.WriteLine(_pointer);
            _pointerValid = true;
        }

        public bool IsPointerValid => _pointerValid;

        public Vector2 Pointer => _pointer;

        public InputButton MainButton => _mainInBtn;

        public InputButton BackButton => _backInBtn;

        public IList<IRayPointer>? Pointers { get; set; }
    }
}
