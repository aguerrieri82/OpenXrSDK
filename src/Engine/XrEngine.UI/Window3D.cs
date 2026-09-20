using CanvasUI;
using Microsoft.Extensions.Hosting;
using SkiaSharp;
using System.Numerics;
using XrInteraction;

namespace XrEngine.UI
{
    public class Window3D : CanvasView3D, IUiWindow
    {
        protected QuadCollider _collider;
        protected Vector2 _lastPosition;
        protected ISurfaceInput _input;

        public Window3D()
        {
            _collider = this.AddComponent<QuadCollider>();
            _input = this.AddComponent<SurfaceController>();
            _lastPosition.X = float.NaN;
        }


        public override void Update(RenderContext ctx)
        {
            ProcessInput();

            base.Update(ctx);
        }

        protected void ProcessInput()
        {
            if (!_input.IsPointerValid)
                return;

            DispatchPointerEvent(_input.Position, _input.MainButton.IsChanged ? Pointer2Button.Left : Pointer2Button.None, UiEventType.PointerMove);

            if (_input.MainButton.IsChanged)
            {
                if (_input.MainButton.IsDown)
                    DispatchPointerEvent(_input.Position, Pointer2Button.Left, UiEventType.PointerDown);
                else
                    DispatchPointerEvent(_input.Position, Pointer2Button.Left, UiEventType.PointerUp);
            }
        }


        private void DispatchPointerEvent(Vector2 surfacePos, Pointer2Button buttons, UiEventType type)
        {
            if (Content == null || _input.Pointer == null)
                return;

            var pos = new Vector2(
                _pixelSize.Width / _dpiScale * surfacePos.X,
                _pixelSize.Height / _dpiScale * (1 - surfacePos.Y)
            );

            var pointer = _input.Pointer;

            var capture = UiManager.GetPointerCapture(pointer.PointerId);

            IUiPointer? uiPointer = null;

            if (pointer is IRayPointer ray)
                uiPointer = new UiRayPointer(ray);
            
            else if (pointer is ITouchPointer touch)
                uiPointer = new UiTouchPointer(touch, buttons == Pointer2Button.Left);

            if (capture != null)
            {
                var uiEv = UiManager.AcquireEvent<UiPointerEvent>();

                uiEv.Buttons = (UiPointerButton)buttons;
                uiEv.Pointer = uiPointer;
                uiEv.WindowPosition = pos;
                uiEv.Type = type;
                uiEv.Source = capture;
                uiEv.Dispatch = UiEventDispatch.Direct;
                capture.DispatchEvent(uiEv);

            }
            else
            {
                var hitTest = Content.HitTest(pos);

                UiManager.SetHoverElement(hitTest, pos, (UiPointerButton)buttons);

                if (hitTest != null)
                {
                    var uiEv = UiManager.AcquireEvent<UiPointerEvent>();

                    uiEv.Buttons = (UiPointerButton)buttons;
                    uiEv.Pointer = uiPointer;
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

        protected override void Draw(SKCanvas canvas, RenderContext? ctx, int activeEye)
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


    }
}
