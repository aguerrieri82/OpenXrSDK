using CanvasUI;
using SkiaSharp;
using System.Numerics;
using XrEngine.Interaction;

namespace XrEngine.UI
{
    public class Window3D : CanvasView3D, IUiWindow
    {
        protected RayPointerStatus _lastStatus;
        protected MeshCollider _collider;
        protected Vector2 _lastPosition;

        public Window3D()
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

                    DispatchPointerEvent(pos, status.Buttons, UiEventType.PointerMove, pointer.Id);

                    _lastPosition = pos;
                }

                foreach (var button in Enum.GetValues<PointerButton>())
                {
                    var isOn = (status.Buttons & button) == button;
                    var wasOn = (_lastStatus.Buttons & button) == button;

                    if (isOn && !wasOn)
                        DispatchPointerEvent(_lastPosition, button, UiEventType.PointerDown, pointer.Id);

                    if (!isOn && wasOn)
                        DispatchPointerEvent(_lastPosition, button, UiEventType.PointerUp, pointer.Id);
                }

                _lastStatus = status;
            }
        }


        private void DispatchPointerEvent(Vector2 uv, PointerButton buttons, UiEventType type, int id)
        {
            if (Content == null)
                return;

            var pos = new Vector2(
                _pixelSize.Width / _dpiScale * uv.X,
                _pixelSize.Height / _dpiScale * uv.Y
            );

            var capture = UiManager.GetPointerCapture(id);

            if (capture != null)
            {
                var uiEv = new UiPointerEvent
                {
                    Buttons = (UiPointerButton)buttons,
                    Pointer = new UiDefaultPointer(id),
                    WindowPosition = pos,
                    Type = type,
                    Source = capture,
                    Dispatch = UiEventDispatch.Direct
                };

                capture.DispatchEvent(uiEv);
            }
            else
            {
                var hitTest = Content.HitTest(pos);

                UiManager.SetHoverElement(hitTest, pos, (UiPointerButton)buttons);

                if (hitTest != null)
                {
                    var uiEv = new UiPointerEvent
                    {
                        Buttons = (UiPointerButton)buttons,
                        Pointer = new UiDefaultPointer(id),
                        WindowPosition = pos,
                        Type = type,
                        Source = hitTest,
                        Dispatch = UiEventDispatch.Bubble
                    };

                    hitTest.DispatchEvent(uiEv);
                }
            }
        }

        protected override void UpdateSize()
        {
            base.UpdateSize();

            //TODO rethink UIRoot
            (Content as UIRoot)?.SetViewport(0, 0, _pixelSize.Width / _dpiScale, _pixelSize.Height / _dpiScale);
        }

        protected override void Draw(SKCanvas canvas)
        {
            if (Content != null && Content.IsDirty)
            {
                canvas.Clear();
                Content.Draw(canvas);
            }
        }

        void IUiWindow.Close()
        {
            _parent?.RemoveChild(this);
        }

        Vector3 IUiWindow.Position 
        { 
            get => WorldPosition;
            set => WorldPosition = value;
        }

        public override bool NeedDraw => Content != null && Content.IsDirty;

        public UiElement? Content { get; set; }

        public IRayPointer[]? Pointers { get; set; }

    }
}
