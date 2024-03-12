using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using XrEngine.UI.Components;
using XrMath;


namespace XrEngine.UI
{
    public abstract class UiComponent : UiObject, ICanvasDraw
    {
        static readonly UiProperty<UiStyle> StyleProp = CreateProp<UiStyle>(nameof(Style), typeof(UiComponent));
        
        protected bool _isDirty;
        protected bool _isLayoutDirty;
        protected UiPanel? _parent;
        protected Rect2 _clientRect;
        protected Size2 _desiredSize;

        public UiComponent()
        {
            Style = new UiStyle(this);
        }

        public void Measure(Size2 availSize)
        {
            var newSize = availSize;

            if (Style.Width.Mode == UiStyleMode.Value)
                newSize.Width = Style.Width.ActualValue(this).ToPixel(this);

            if (Style.Height.Mode == UiStyleMode.Value)
                newSize.Height = Style.Height.ActualValue(this).ToPixel(this);

            var padding = Style.Padding.ActualValue(this);
            var margin = Style.Margin.ActualValue(this);    

            newSize.Width -= padding.ToHorizontalPixel(this);
            newSize.Height -= padding.ToVerticalPixel(this);

            newSize.Width -= margin.ToHorizontalPixel(this);
            newSize.Height -= margin.ToVerticalPixel(this);

            _desiredSize = MeasureWork(newSize);   
        }

        public void Arrange(Rect2 finalRect)
        {
            var newRect = finalRect;


            if (Style.Top.Mode == UiStyleMode.Value)
                newRect.Top += Style.Top.ActualValue(this).ToPixel(this);

            if (Style.Left.Mode == UiStyleMode.Value)
                newRect.Left += Style.Left.ActualValue(this).ToPixel(this);

            var margin = Style.Margin.ActualValue(this);

            newRect.Left += margin.Left.ToPixel(this);
            newRect.Right -= margin.Right.ToPixel(this);

            newRect.Top += margin.Top.ToPixel(this);
            newRect.Bottom -= margin.Bottom.ToPixel(this);;
            
            ArrangeWork(newRect);

            _isLayoutDirty = false;
            _clientRect = finalRect;
        }

        protected virtual void ArrangeWork(Rect2 finalRect)
        {
        }

        protected virtual Size2 MeasureWork(Size2 availSize)
        {
            return availSize;
        }

        protected override void OnPropertyChanged<T>(UiProperty<T> prop, T? value, T? oldValue) where T : default
        {
            if ((prop.Flags & UiPropertyFlags.Layout) == UiPropertyFlags.Layout)
                InvalidateLayout();

            IsDirty = true;
        }

        protected virtual void InvalidateLayout()
        {
            _isLayoutDirty = true;
            _parent?.InvalidateLayout();
        }

        protected internal void OnStyleChanged<T>(UiProperty<T> prop, T? value, T? oldValue)
        {
            if ((prop.Flags & UiPropertyFlags.Layout) == UiPropertyFlags.Layout)
                InvalidateLayout();
        }

        protected virtual void DrawBox(SKCanvas canvas)
        {
            var bkColor = Style.BackgroundColor.ActualValue(this);
            if (bkColor != null)
            {
                var paint = SKResources.FillColor(bkColor.Value);
                canvas.DrawRect(_clientRect.X, _clientRect.Y, _clientRect.Width, _clientRect.Height, paint);
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

        public UiPanel? Parent
        {
            get => _parent;

            internal set
            {
                _parent = value;    
            }
        }

        public UiStyle Style
        {
            get => GetValue(StyleProp)!;
            set => SetValue(StyleProp, value);
        }

        public Size2 DesiredSize => _desiredSize;

        public Rect2 ClientRect => _clientRect;
    }
}
