using CanvasUI;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using System.Numerics;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;

namespace UI.Canvas.Wpf
{
    public class UiElementView : SKElement
    {
        public static readonly DependencyProperty ContentProperty =
                DependencyProperty.Register("Content", typeof(UiElement), typeof(UiElementView), new PropertyMetadata(null, OnContentChanged));

        UIRoot _root;

        struct Pointer : IUiPointer
        {
            UiElementView _element;

            public Pointer(UiElementView element, int id)
            {
                Id = id;
                _element = element;
            }

            public void Capture(UiElement element)
            {
                _element.CaptureMouse();
                UiManager.SetPointerCapture(Id, element);
            }

            public void Release()
            {
                _element.ReleaseMouseCapture();
                UiManager.SetPointerCapture(Id, null);
            }

            public int Id { get; }
        }

        public UiElementView()
        {
            _root = new UIRoot();
            _root.NeedRedraw += OnNeedRedraw;
            _root.BuildStyle(a => a.Padding(16));
        }

        private void OnNeedRedraw(object? sender, EventArgs e)
        {
            InvalidateSafe();
        }

        public static void OnContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (UiElementView)d;
            view.OnContentChanged(e.NewValue as UiElement, e.OldValue as UiElement);
        }

        public UiElement? Content
        {
            get { return (UiElement?)GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }


        protected virtual void OnContentChanged(UiElement? newContent, UiElement? oldContent)
        {
            _root.Clear();

            if (newContent != null)
                _root.AddChild(newContent);

            InvalidateSafe();
        }

        protected void InvalidateSafe()
        {
            if (Application.Current.Dispatcher.Thread == Thread.CurrentThread)
                InvalidateVisual();
            else
                Application.Current.Dispatcher.Invoke(InvalidateVisual);
        }

        protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
        {
            e.Surface.Canvas.Clear();
            _root.SetViewport(0, 0, e.Info.Width, e.Info.Height);
            _root.Draw(e.Surface.Canvas);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            var button = e.ChangedButton switch
            {
                MouseButton.Left => UiPointerButton.Left,
                MouseButton.Right => UiPointerButton.Right,
                MouseButton.Middle => UiPointerButton.Middle,
                _ => UiPointerButton.None
            };

            if (button != UiPointerButton.None)
                DispatchPointerEvent(e, UiEventType.PointerDown, button);

            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            var buttons = UiPointerButton.None;
            
            if (e.MiddleButton == MouseButtonState.Pressed)
                buttons |= UiPointerButton.Middle;
            
            if (e.LeftButton == MouseButtonState.Pressed)
                buttons |= UiPointerButton.Left;

            if (e.RightButton == MouseButtonState.Pressed)
                buttons |= UiPointerButton.Right;


            DispatchPointerEvent(e, UiEventType.PointerMove, buttons);
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            var button = e.ChangedButton switch
            {
                MouseButton.Left => UiPointerButton.Left,
                MouseButton.Right => UiPointerButton.Right,
                MouseButton.Middle => UiPointerButton.Middle,
                _ => UiPointerButton.None
            };

            if (button != UiPointerButton.None)
                DispatchPointerEvent(e, UiEventType.PointerUp, button);

            base.OnMouseUp(e);
        }

        private void DispatchPointerEvent(MouseEventArgs args, UiEventType type, UiPointerButton buttons)
        {
            if (Content == null)
                return;

            var scale = new Vector2(_root.ActualWidth / (float)ActualWidth, _root.ActualHeight / (float)ActualHeight);

            var relPos = args.GetPosition(this);

            var pos = new Vector2((float)relPos.X, (float)relPos.Y) * scale;

            var capture = UiManager.GetPointerCapture(0);

            if (capture != null)
            {
                var uiEv = UiManager.AcquireEvent<UiPointerEvent>();

                uiEv.Buttons = buttons;
                uiEv.Pointer = new Pointer(this, 0);
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

                    uiEv.Buttons = buttons;
                    uiEv.Pointer = new Pointer(this, 0);
                    uiEv.WindowPosition = pos;
                    uiEv.Type = type;
                    uiEv.Source = hitTest;
                    uiEv.Dispatch = UiEventDispatch.Bubble;

                    hitTest.DispatchEvent(uiEv);
                }
            }

        }

    }
}
