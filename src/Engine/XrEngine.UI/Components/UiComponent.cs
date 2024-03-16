using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Numerics;
using System.Runtime.InteropServices.JavaScript;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.XPath;
using XrEngine.UI.Components;
using XrMath;


namespace XrEngine.UI
{
    public abstract class UiComponent : UiObject, ICanvasDraw, ILayoutItem
    {
        protected bool _isDirty;
        protected bool _isLayoutDirty;
        protected bool _isStyleDirty;

        protected UiContainer? _parent;
        protected Rect2 _clientRect;
        protected Rect2 _contentRect;
        protected Size2 _desiredSize;
        protected Size2 _renderSize;
        protected UiComponent? _host;
        protected UIActualStyle _actualStyle;

        public UiComponent()
        {
            _actualStyle = new UIActualStyle(this) { BaseStyle = () => Style };
            Style = new UiStyle(this);
        }

        #region LAYOUT

        protected void ApplySizeLimit(ref Rect2 rect)
        {
            if (ActualStyle.Width.HasValue)
                rect.Width = ActualStyle.Width.ToPixel(this, UiValueReference.ParentWidth);

            if (ActualStyle.MinWidth.HasValue)
                rect.Width = Math.Max(ActualStyle.MinWidth.ToPixel(this, UiValueReference.ParentWidth), rect.Width);

            if (ActualStyle.MaxWidth.HasValue)
                rect.Width = Math.Min(ActualStyle.MaxWidth.ToPixel(this, UiValueReference.ParentWidth), rect.Width);

            if (ActualStyle.Height.HasValue)
                rect.Height = ActualStyle.Height.ToPixel(this, UiValueReference.ParentHeight);

            if (ActualStyle.MinHeight.HasValue)
                rect.Height = Math.Max(ActualStyle.MinHeight.ToPixel(this, UiValueReference.ParentHeight), rect.Height);

            if (ActualStyle.MaxHeight.HasValue)
                rect.Height = Math.Min(ActualStyle.MaxHeight.ToPixel(this, UiValueReference.ParentHeight), rect.Height);
        }

        protected void ApplyOffset(ref Rect2 rect, UnitRectValue value, float dir = 1)
        {
            rect.Left += value.Left.ToPixel(this, UiValueReference.ParentWidth) * dir;
            rect.Right -= value.Right.ToPixel(this, UiValueReference.ParentWidth) * dir;
            rect.Top += value.Top.ToPixel(this, UiValueReference.ParentHeight) * dir;
            rect.Bottom -= value.Bottom.ToPixel(this, UiValueReference.ParentHeight) * dir;
        }

        public void Measure(Size2 availSize)
        {
            if (ActualStyle.Visibility == UiVisibility.Collapsed)
            {
                _desiredSize = Size2.Zero;
                return;
            }

            var hasFixedSize = ActualStyle.Width.HasValue && ActualStyle.Height.HasValue;

            var padding = ActualStyle.Padding.Value;
            var margin = ActualStyle.Margin.Value;
            var border = ActualStyle.Border.Value;

            var measureRect = new Rect2(0, 0, availSize.Width, availSize.Height);

            ApplySizeLimit(ref measureRect);

            if (!hasFixedSize)
            {    
                ApplyOffset(ref measureRect, padding);
                ApplyOffset(ref measureRect, border);

                measureRect.Size = MeasureWork(measureRect.Size);

                ApplyOffset(ref measureRect, padding, -1);
                ApplyOffset(ref measureRect, border, -1);

                ApplySizeLimit(ref measureRect);
            }

            ApplyOffset(ref measureRect, margin, -1);

            _desiredSize = measureRect.Size;
        }

        public void Arrange(Rect2 finalRect)
        {
            _clientRect = finalRect;

            ApplyOffset(ref _clientRect, ActualStyle.Margin.Value);

            _contentRect = _clientRect;

            ApplyOffset(ref _contentRect, ActualStyle.Padding.Value);
            ApplyOffset(ref _contentRect, ActualStyle.Border.Value);

            _renderSize = ArrangeWork(_contentRect);

            _isLayoutDirty = false;
        }

        protected virtual Size2 ArrangeWork(Rect2 finalRect)
        {
            return finalRect.Size;
        }

        protected virtual Size2 MeasureWork(Size2 availSize)
        {
            return availSize;
        }

        protected virtual void InvalidateLayout()
        {
            _isLayoutDirty = true;
            VisualParent?.InvalidateLayout();
        }

        Size2 ILayoutItem.Measure(Size2 size)
        {
            Measure(size);
            return _desiredSize;
        }

        Size2 ILayoutItem.Arrange(Rect2 finalRect)
        {
            Arrange(finalRect);
            return _clientRect.Size;
        }

        #endregion

        #region EVENTS

        public void DispatchEvent(UiRoutedEvent ev)
        {
            switch (ev.Type)
            {
                case UiEventType.PointerDown:
                    OnPointerDown((UiPointerEvent)ev);
                    break;
                case UiEventType.PointerUp:
                    OnPointerUp((UiPointerEvent)ev);
                    break;
                case UiEventType.PointerMove:
                    OnPointerMove((UiPointerEvent)ev);
                    break;
                case UiEventType.GotFocus:
                    OnGotFocus(ev);
                    break;
                case UiEventType.LostFocus:
                    OnLostFocus(ev);
                    break;
                case UiEventType.PointerEnter:
                    OnPointerEnter((UiPointerEvent)ev);
                    break;
                case UiEventType.PointerLeave:
                    OnPointerLeave((UiPointerEvent)ev);
                    break;
            }

            if (!ev.StopBubble && ev.Dispatch == UiEventDispatch.Bubble)
            {
                VisualParent?.DispatchEvent(ev);
            }
            else if (ev.Dispatch == UiEventDispatch.Tunnel)
            {
                foreach (var child in VisualChildren)
                    child.DispatchEvent(ev);
            }
        }

        protected virtual void OnPointerEnter(UiPointerEvent ev)
        {
            EnableState(UiControlState.Hover, true);
            PointerEnter?.Invoke(this, ev);
        }

        protected virtual void OnPointerLeave(UiPointerEvent ev)
        {
            EnableState(UiControlState.Hover, false);
            PointerLeave?.Invoke(this, ev);
        }

        protected virtual void OnGotFocus(UiRoutedEvent ev)
        {
            EnableState(UiControlState.Focused, true);
            GotFocus?.Invoke(this, ev);
        }

        protected virtual void OnLostFocus(UiRoutedEvent ev)
        {
            EnableState(UiControlState.Focused, false);
            LostFocus?.Invoke(this, ev);
        }

        protected virtual void OnPointerDown(UiPointerEvent ev)
        {
            if (IsFocusable)
                UiFocusManager.SetFocus(this);
            PointerDown?.Invoke(this, ev);  
        }

        protected virtual void OnPointerMove(UiPointerEvent ev)
        {
            PointerMove?.Invoke(this, ev);
        }

        protected virtual void OnPointerUp(UiPointerEvent ev)
        {
            PointerUp?.Invoke(this, ev);
        }

        public event UiEventHandler<UiPointerEvent>? PointerDown;

        public event UiEventHandler<UiPointerEvent>? PointerMove;

        public event UiEventHandler<UiPointerEvent>? PointerUp;

        public event UiEventHandler<UiPointerEvent>? PointerEnter;

        public event UiEventHandler<UiPointerEvent>? PointerLeave;

        public event UiEventHandler<UiRoutedEvent>? GotFocus;

        public event UiEventHandler<UiRoutedEvent>? LostFocus;


        #endregion

        #region PROPS

        protected override void OnPropertyChanged(string propName, object? value, object? oldValue)
        {
            var prop = GetProperty(propName, GetType());

            if ((prop.Flags & UiPropertyFlags.Layout) == UiPropertyFlags.Layout)
                InvalidateLayout();

            IsDirty = true;

            base.OnPropertyChanged(propName, value, oldValue);
        }

        protected internal void OnStyleChanged(string propName, IUiStyleValue value, IUiStyleValue oldValue)
        {
            var prop = GetProperty(propName, typeof(UiStyle));
            if ((prop.Flags & UiPropertyFlags.Layout) == UiPropertyFlags.Layout)
                InvalidateLayout();
        }

        #endregion

        #region RENDER

        protected virtual void DrawBox(SKCanvas canvas)
        {
            var bkColor = ActualStyle.BackgroundColor.Value;
            if (bkColor != null)
            {
                var paint = SKResources.FillColor(bkColor.Value);
                canvas.DrawRect(_clientRect.X, _clientRect.Y, _clientRect.Width, _clientRect.Height, paint);
            }

            var border = ActualStyle.Border.Value;
            if (border.Top.HasValue)
            {
                var paint = SKResources.Stroke(border.Top.Color, border.Top.Width.ToPixel(this, UiValueReference.ParentWidth));
                canvas.DrawLine(_clientRect.X, _clientRect.Y, _clientRect.Right, _clientRect.Y, paint);
            }
            if (border.Bottom.HasValue)
            {
                var paint = SKResources.Stroke(border.Bottom.Color, border.Bottom.Width.ToPixel(this, UiValueReference.ParentWidth));
                canvas.DrawLine(_clientRect.X, _clientRect.Bottom, _clientRect.Right, _clientRect.Bottom, paint);
            }
            if (border.Left.HasValue)
            {
                var paint = SKResources.Stroke(border.Left.Color, border.Left.Width.ToPixel(this, UiValueReference.ParentHeight));
                canvas.DrawLine(_clientRect.X, _clientRect.Y, _clientRect.X, _clientRect.Bottom, paint);
            }
            if (border.Right.HasValue)
            {
                var paint = SKResources.Stroke(border.Right.Color, border.Right.Width.ToPixel(this, UiValueReference.ParentHeight));
                canvas.DrawLine(_clientRect.Right, _clientRect.Y, _clientRect.Right, _clientRect.Bottom, paint);
            }
        }

        public virtual void Draw(SKCanvas canvas)
        {
            if (ActualStyle.Visibility != UiVisibility.Visible)
                return;

            canvas.Save();
            DrawBox(canvas);
            DrawWork(canvas);
            canvas.Restore();
            _isDirty = false;
        }

        protected abstract void DrawWork(SKCanvas canvas);

        #endregion

        #region STYLE

        protected void EnableState(UiControlState state, bool enable)
        {
            if (enable)
                State |= state;
            else
                State &= ~state;
        }

        public UiStyle StateActualStyle(UiControlState state)
        {
            var propName = string.Concat("_ActualStyle", state);
            var value = GetValue<UiStyle>(propName);
            if (value == null)
            {
                var stylePropName = string.Concat("_Style", state);

                var styleValue = GetValue<UiStyle>(stylePropName);
                if (styleValue == null)
                    return _actualStyle;

                value = new UIActualStyle(this) { BaseStyle = () => styleValue };

                SetValue(propName, value);
            }

            return value;
        }

        public UiStyle StateStyle(UiControlState state)
        {
            var propName = string.Concat("_Style", state);
            var value = GetValue<UiStyle>(propName);
            if (value == null)
            {
                value = new UiStyle(this) { BaseStyle = () => Style };
                SetValue(propName, value);
            }
            return value;
        }


        #endregion

        public bool IsDirty
        {
            get => _isDirty;

            internal protected set
            {
                _isDirty = value;

                if (_parent != null)
                    _parent.IsDirty = true;
            }
        }

        public UiContainer? Parent
        {
            get => _parent;

            internal set
            {
                _parent = value;    
            }
        }

        public UiComponent? Host
        {
            get => _host;

            internal set
            {
                _host = value;
            }
        }

        [UiProperty(false)]       
        public bool IsFocusable
        {
            get => GetValue<bool>(nameof(IsFocusable))!;
            set => SetValue(nameof(IsFocusable), value);
        }

        public UiStyle Style
        {
            get => GetValue<UiStyle>(nameof(Style))!;
            set => SetValue(nameof(Style), value);
        }

        public UiControlState State
        {
            get => GetValue<UiControlState>(nameof(State))!;
            protected set => SetValue(nameof(State), value);
        }

        public UiStyle ActualStyle => _actualStyle;

        public Size2 DesiredSize => _desiredSize;

        public Size2 RenderSize => _renderSize;

        public float ActualWidth => _clientRect.Width;

        public float ActualHeight => _clientRect.Height;

        public Rect2 ClientRect => _clientRect;

        public UiComponent? VisualParent => _parent ?? _host;

        public virtual IEnumerable<UiComponent> VisualChildren => [];

    }
}
