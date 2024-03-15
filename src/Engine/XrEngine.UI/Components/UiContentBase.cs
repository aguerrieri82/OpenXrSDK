using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using XrEngine.UI.Components;
using XrMath;

namespace XrEngine.UI
{
    public abstract class UiContentBase : UiComponent
    {
 
        protected UiComponent? _content;

        protected UiComponent? GetContent()
        {
            if (Content == null)
                return null;
            if (Content is UiComponent comp)
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


        public object? Content
        {
            get => GetValue<object?>(nameof(Content))!;
            set => SetValue(nameof(Content), value);
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
            throw new NotImplementedException();
        }
    }
}
