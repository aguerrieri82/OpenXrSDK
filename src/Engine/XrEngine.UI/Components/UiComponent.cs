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
        protected UiComponent? _host;
        protected UIActualStyle _actualStyle;

        public UiComponent()
        {
            _actualStyle = new UIActualStyle(this) { BaseStyle = () => Style };
            Style = new UiStyle(this);
        }

        public void Measure(Size2 availSize)
        {
            var contentSize = availSize;

            if (ActualStyle.Width.Mode == UiStyleMode.Value)
                contentSize.Width = ActualStyle.Width.ToPixel(this, contentSize.Width);

            if (ActualStyle.Height.Mode == UiStyleMode.Value)
                contentSize.Height = ActualStyle.Height.ToPixel(this, contentSize.Height);

            var padding = ActualStyle.Padding.Value;
            var margin = ActualStyle.Margin.Value;
            var border = ActualStyle.Border.Value;

            contentSize.Width -= padding.ToHorizontalPixel(this) + 
                             margin.ToHorizontalPixel(this) + 
                             border.Left.Width.ToPixel(this) + 
                             border.Right.Width.ToPixel(this);

            contentSize.Height -= padding.ToVerticalPixel(this) +
                              margin.ToVerticalPixel(this) + 
                              border.Top.Width.ToPixel(this) + 
                              border.Bottom.Width.ToPixel(this);

            _desiredSize = MeasureWork(contentSize);   
        }

        public void Arrange(Rect2 finalRect)
        {
            _clientRect = finalRect;

            /*
            if (ActualStyle.Top.Mode == UiStyleMode.Value)
                _clientRect.Top += ActualStyle.Top.Value.ToPixel(this, finalRect.Height);

            if (ActualStyle.Left.Mode == UiStyleMode.Value)
                _clientRect.Left += ActualStyle.Left.Value.ToPixel(this, finalRect.Width);
            */

            var margin = ActualStyle.Margin.Value;

            _clientRect.Left += margin.Left.ToPixel(this, finalRect.Width);
            _clientRect.Right -= margin.Right.ToPixel(this, finalRect.Width);

            _clientRect.Top += margin.Top.ToPixel(this, finalRect.Height);
            _clientRect.Bottom -= margin.Bottom.ToPixel(this, finalRect.Height);;

            var padding = ActualStyle.Padding.Value;
            var border = ActualStyle.Border.Value;

            _contentRect = _clientRect;

            _contentRect.Left += padding.Left.ToPixel(this, finalRect.Width) + border.Left.Width.ToPixel(this, finalRect.Width);
            _contentRect.Right -= padding.Right.ToPixel(this, finalRect.Width) + border.Right.Width.ToPixel(this, finalRect.Width);

            _contentRect.Top += padding.Top.ToPixel(this, finalRect.Height) + border.Top.Width.ToPixel(this, finalRect.Height);
            _contentRect.Bottom -= padding.Bottom.ToPixel(this, finalRect.Height) + border.Bottom.Width.ToPixel(this, finalRect.Height);

            var newSize = ArrangeWork(_contentRect);

            var delta = newSize - _contentRect.Size;

            _contentRect.Expand(delta);

            _clientRect.Expand(delta);

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

        protected override void OnPropertyChanged(string propName, object? value, object? oldValue)
        {
            var prop = GetProperty(propName, GetType());

            if ((prop.Flags & UiPropertyFlags.Layout) == UiPropertyFlags.Layout)
                InvalidateLayout();

            IsDirty = true;

            base.OnPropertyChanged(propName, value, oldValue);
        }

        protected virtual void InvalidateLayout()
        {
            _isLayoutDirty = true;
            _parent?.InvalidateLayout();
        }

        protected internal void OnStyleChanged(string propName, IUiStyleValue value, IUiStyleValue oldValue)
        {
            var prop = GetProperty(propName, typeof(UiStyle));
            if ((prop.Flags & UiPropertyFlags.Layout) == UiPropertyFlags.Layout)
                InvalidateLayout();
        }

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
                var paint = SKResources.Stroke(border.Top.Color, border.Top.Width.ToPixel(this));
                canvas.DrawLine(_clientRect.X, _clientRect.Y, _clientRect.Right, _clientRect.Y, paint);
            }
            if (border.Bottom.HasValue)
            {
                var paint = SKResources.Stroke(border.Bottom.Color, border.Bottom.Width.ToPixel(this));
                canvas.DrawLine(_clientRect.X, _clientRect.Bottom, _clientRect.Right, _clientRect.Bottom, paint);
            }
            if (border.Left.HasValue)
            {
                var paint = SKResources.Stroke(border.Left.Color, border.Left.Width.ToPixel(this));
                canvas.DrawLine(_clientRect.X, _clientRect.Y, _clientRect.X, _clientRect.Bottom, paint);
            }
            if (border.Right.HasValue)
            {
                var paint = SKResources.Stroke(border.Right.Color, border.Right.Width.ToPixel(this));
                canvas.DrawLine(_clientRect.Right, _clientRect.Y, _clientRect.Right, _clientRect.Bottom, paint);
            }
        }

        public virtual void Draw(SKCanvas canvas)
        {
            canvas.Save();
            DrawBox(canvas);
            DrawWork(canvas);
            canvas.Restore();
            _isDirty = false;
        }

        protected abstract void DrawWork(SKCanvas canvas);

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

        public UiStyle Style
        {
            get => GetValue<UiStyle>(nameof(Style))!;
            set => SetValue(nameof(Style), value);
        }

        public UiStyle ActualStyle => _actualStyle;

        public Size2 DesiredSize => _desiredSize;

        public float ActualWidth => _clientRect.Width;

        public float ActualHeight => _clientRect.Height;

        public Rect2 ClientRect => _clientRect;
    }
}
