using CanvasUI;
using SkiaSharp;
using System.Numerics;
using XrEngine.Interaction;

namespace XrEngine.UI
{
    public class Panel3D : CanvasView3D
    {
        protected RayPointerStatus _lastStatus;
        protected MeshCollider _collider;
        protected Vector2 _lastPosition;

        public Panel3D()
        {
            _collider = this.AddComponent<MeshCollider>();
            _lastPosition.X = float.NaN;
        }

        protected override void Start(RenderContext ctx)
        {
            Pointers ??= Scene?
                .Components<IComponent>()
                .OfType<IRayPointer>()
                .ToArray();
        }

        public override void Update(RenderContext ctx)
        {
            ProcessPointers();

            base.Update(ctx);
        }

        protected void ProcessPointers()
        {
            if (Pointers == null)
                return;

            foreach (var pointer in Pointers)
            {
                var status = pointer.GetPointerStatus();

                if (!status.IsActive)
                    continue;

                var collision = _collider.CollideWith(status.Ray);

                if (collision != null)
                {
                    var pos = new Vector2(collision.LocalPoint.X + 0.5f, 1 - (collision.LocalPoint.Y + 0.5f));

                    DispatchPointerEvent(pos, status.Buttons, UiEventType.PointerMove);

                    _lastPosition = pos;
                }

                foreach (var button in Enum.GetValues<PointerButton>())
                {
                    var isOn = (status.Buttons & button) == button;
                    var wasOn = (_lastStatus.Buttons & button) == button;

                    if (isOn && !wasOn)
                        DispatchPointerEvent(_lastPosition, button, UiEventType.PointerDown);

                    if (!isOn && wasOn)
                        DispatchPointerEvent(_lastPosition, button, UiEventType.PointerUp);
                }

                _lastStatus = status;
            }
        }


        private void DispatchPointerEvent(Vector2 uv, PointerButton buttons, UiEventType type)
        {
            if (Panel == null)
                return;

            var pos = new Vector2(
                _pixelSize.Width / _dpiScale * uv.X,
                _pixelSize.Height / _dpiScale * uv.Y
            );

            var hitTest = Panel.HitTest(pos);

            UiFocusManager.SetHoverElement(hitTest, pos, (UiPointerButton)buttons);

            if (hitTest != null)
            {
                var uiEv = new UiPointerEvent
                {
                    Buttons = (UiPointerButton)buttons,
                    ScreenPosition = pos,
                    Type = type,
                    Source = hitTest,
                    Dispatch = UiEventDispatch.Bubble
                };

                hitTest.DispatchEvent(uiEv);
            }
        }

        protected override void UpdateSize()
        {
            base.UpdateSize();

            Panel?.SetViewport(0, 0, _pixelSize.Width / _dpiScale, _pixelSize.Height / _dpiScale);
        }

        protected override void Draw(SKCanvas canvas)
        {
            if (Panel != null && Panel.IsDirty)
            {
                canvas.Clear();
                Panel.Draw(canvas);
            }
        }

        public override bool NeedDraw => Panel != null && Panel.IsDirty;

        public UIRoot? Panel { get; set; }

        public IRayPointer[]? Pointers { get; set; }
    }
}
