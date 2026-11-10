using CanvasUI;
using SkiaSharp;
using XrEngine;
using XrEngine.UI;

namespace XrSamples.Graffiti
{
    public class CapLabel : CanvasView3D
    {
        string _text = "";
        bool _isDirty;
        SKFont? _font;
        SKPaint? _paint;

        public CapLabel()
        {
            _dpi = UnitConv.InchesToMeter * 2048;
        }

        protected override void Draw(SKCanvas canvas, RenderContext? ctx, int activeEye)
        {
            _font ??= new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold), PixelSize.Height * 0.6f);
            _paint ??= new SKPaint { Color = SKColors.White, IsAntialias = true };

            canvas.Clear(new SKColor(0, 0, 0, 160));

            canvas.DrawText(
                _text,
                PixelSize.Width / 2f,
                PixelSize.Height / 2f - (_font.Metrics.Ascent + _font.Metrics.Descent) / 2,
                SKTextAlign.Center,
                _font,
                _paint);

            _isDirty = false;
        }

        public string Text
        {
            get => _text;
            set
            {
                if (_text == value)
                    return;
                _text = value;
                _isDirty = true;
            }
        }

        public override bool NeedDraw => _isDirty;
    }
}
