using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrEngine.UI.Components;
using XrMath;

namespace XrEngine.UI
{
    public class UiTextBlock : UiComponent
    {
        static readonly UiProperty<string> TextProp = CreateProp(nameof(Text), typeof(UiTextBlock), string.Empty);

        public UiTextBlock()
        {

        }

        protected override Size2 MeasureWork(Size2 availSize)
        {
            var fontSize = Style.FontSize.ActualValue(this).ToPixel(this);
            var family = Style.FontFamily.ActualValue(this);
            var font = SKResources.Font(family!, fontSize);

            var width = font.MeasureText(Text);

            return new Size2
            {
                Width = width,
                Height = font.Size
            };
        }

        protected override void DrawWork(SKCanvas canvas)
        {
            var fontSize = Style.FontSize.ActualValue(this).ToPixel(this);
            var family = Style.FontFamily.ActualValue(this);
            var font = SKResources.Font(family!, fontSize);
            var color = Style.Color.ActualValue(this);

            var paint = SKResources.FillColor(color!.Value);

            canvas.DrawText(Text, _clientRect.X, _clientRect.Y + font.Size, font, paint);
        }

        public string Text
        {
            get => GetValue(TextProp)!;
            set => SetValue(TextProp, value);
        }

    }
}
