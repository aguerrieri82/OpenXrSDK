using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using XrMath;

namespace CanvasUI
{
    public class UiContentView : UiElement
    {
        protected UiElement? _content;

        protected virtual UiElement? GetContent()
        {
            if (Content == null)
                return null;
            if (Content is UiElement comp)
                return comp;
            var text = Content.ToString();
            return new UiTextBlock() { Text = text ?? string.Empty };
        }

        protected virtual void OnContentChanged()
        {
            if (_content != null)
                _content.Host = null;

            _content = GetContent();

            if (_content != null)
                _content.Host = this;
        }

        protected override void OnPropertyChanged(string propName, object? value, object? oldValue)
        {
            if (propName == nameof(Content))
                OnContentChanged();

            base.OnPropertyChanged(propName, value, oldValue);
        }

        protected override Size2 ArrangeWork(Rect2 finalRect)
        {
            if (_content != null)
            {
                _content.Arrange(finalRect);
                return _content.RenderSize;
            }
  
            return base.ArrangeWork(finalRect);
        }

        protected override Size2 MeasureWork(Size2 availSize)
        {
            if (_content != null)
            {
                _content.Measure(availSize);
                return _content.DesiredSize;
            }
            return Size2.Zero;
        }

        protected override void DrawWork(SKCanvas canvas)
        {
            _content?.Draw(canvas);
        }

        public override IEnumerable<UiElement> VisualChildren
        {
            get
            {
                if (_content != null)
                    yield return _content;
            }
        }

        [UiProperty(null, UiPropertyFlags.Layout)]
        public object? Content
        {
            get => GetValue<object?>(nameof(Content))!;
            set => SetValue(nameof(Content), value);
        }
    }
}
