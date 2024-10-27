using CanvasUI;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace UI.Canvas.Wpf
{
    public class UiElementView : SKElement
    {
        public static readonly DependencyProperty ContentProperty =
                DependencyProperty.Register("Content", typeof(UiElement), typeof(UiElementView), new PropertyMetadata(0));

        public UiElement? Content
        {
            get { return (UiElement?)GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }


        protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
        {
            Content?.Draw(e.Surface.Canvas);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
        }

        private void DispatchPointerEvent(Vector2 surfacePos, UiPointerButton buttons, UiEventType type, IUiPointer pointer)
        {
            if (Content == null)
                return;

            /*
            var pos = new Vector2(
                _pixelSize.Width / _dpiScale * surfacePos.X,
                _pixelSize.Height / _dpiScale * surfacePos.Y
            );

            var capture = UiManager.GetPointerCapture(pointer.PointerId);

            if (capture != null)
            {
                var uiEv = UiManager.AcquireEvent<UiPointerEvent>();

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
                var hitTest = Content.HitTest(pos);

                UiManager.SetHoverElement(hitTest, pos, (UiPointerButton)buttons);

                if (hitTest != null)
                {
                    var uiEv = UiManager.AcquireEvent<UiPointerEvent>();

                    uiEv.Buttons = (UiPointerButton)buttons;
                    uiEv.Pointer = new UiRayPointer(pointer);
                    uiEv.WindowPosition = pos;
                    uiEv.Type = type;
                    uiEv.Source = hitTest;
                    uiEv.Dispatch = UiEventDispatch.Bubble;

                    hitTest.DispatchEvent(uiEv);
                }
            }
            */
        }

    }
}
