using CanvasUI;
using SkiaSharp;
using System.Numerics;
using XrInteraction;

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

        static readonly Pointer2Button[] BUTTONS = [Pointer2Button.Left, Pointer2Button.Right];

        protected void ProcessPointers()
        {
            if (Pointers == null)
                return;

            foreach (IRayPointer pointer in Pointers)
            {
                RayPointerStatus status = pointer.GetPointerStatus();

                if (!status.IsActive)
                    continue;

                Collision? collision = _collider.CollideWith(status.Ray);

                //TODO infinite plane collision

                if (collision != null)
                {
                    Vector2 pos = new Vector2(collision.LocalPoint.X + 0.5f, 1 - (collision.LocalPoint.Y + 0.5f));

                    DispatchPointerEvent(pos, status.Buttons, UiEventType.PointerMove, pointer);

                    _lastPosition = pos;
                }

                foreach (Pointer2Button button in BUTTONS)
                {
                    bool isOn = (status.Buttons & button) == button;
                    bool wasOn = (_lastStatus.Buttons & button) == button;

                    if (isOn && !wasOn)
                        DispatchPointerEvent(_lastPosition, button, UiEventType.PointerDown, pointer);

                    if (!isOn && wasOn)
                        DispatchPointerEvent(_lastPosition, button, UiEventType.PointerUp, pointer);
                }

                _lastStatus = status;
            }
        }


        private void DispatchPointerEvent(Vector2 surfacePos, Pointer2Button buttons, UiEventType type, IRayPointer pointer)
        {
            if (Content == null)
                return;

            Vector2 pos = new Vector2(
                _pixelSize.Width / _dpiScale * surfacePos.X,
                _pixelSize.Height / _dpiScale * surfacePos.Y
            );

            UiElement? capture = UiManager.GetPointerCapture(pointer.PointerId);

            if (capture != null)
            {
                UiPointerEvent uiEv = UiManager.AcquireEvent<UiPointerEvent>();

                uiEv.Buttons = (UiPointerButton)buttons;
                uiEv.Pointer = new UiRayPointer(pointer);
                uiEv.WindowPosition = pos;
                uiEv.Type = type;
                uiEv.Source = capture;
                uiEv.Dispatch = UiEventDispatch.Direct;
                capture.DispatchEvent(uiEv);

            }
            else
            {
                UiElement? hitTest = Content.HitTest(pos);

                UiManager.SetHoverElement(hitTest, pos, (UiPointerButton)buttons);

                if (hitTest != null)
                {
                    UiPointerEvent uiEv = UiManager.AcquireEvent<UiPointerEvent>();

                    uiEv.Buttons = (UiPointerButton)buttons;
                    uiEv.Pointer = new UiRayPointer(pointer);
                    uiEv.WindowPosition = pos;
                    uiEv.Type = type;
                    uiEv.Source = hitTest;
                    uiEv.Dispatch = UiEventDispatch.Bubble;

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
