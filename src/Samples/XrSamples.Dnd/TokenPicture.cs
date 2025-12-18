
using CanvasUI;
using SkiaSharp;
using XrEngine;
using XrEngine.UI;
using XrMath;

namespace XrSamples.Dnd
{
    public partial class Token
    {
        public class TokenPicture : CanvasView3D
        {
            SKBitmap? _image;
            VttToken? _vttToken;
            bool _isDirty;
            SKFont? _font1;
            SKFont? _font2;
            SKPaint? _white;

            public TokenPicture()
            {
                _dpi = UnitConv.InchesToMeter * 256;
                Size = new Size2(1f, 1f);

            }

            internal void Update(VttToken vttToken)
            {
                _vttToken = vttToken;
                _isDirty = true;

            }

            protected override void Start(RenderContext ctx)
            {

                base.Start(ctx);
            }

            protected override void Draw(SKCanvas canvas)
            {
                canvas.Clear();

                _image ??= ((DndScene?)Scene)!.VttClient.DownloadImageAsync(_vttToken!.Imgsrc!).Result;

                int height = (int)(PixelSize.Height * 0.1);
                int barHeight = (int)(PixelSize.Height * 0.05);

                canvas.Save();


                if (_vttToken!.TokenStyleSelect == "circle")
                {
                    using SKPath path = new SKPath();
                    int radius = height / 2;
                    path.AddOval(new SKRect(0, 0, PixelSize.Width, PixelSize.Height - height - barHeight));
                    canvas.ClipPath(path);
                }

                canvas.DrawBitmap(_image, new SKRect(0, 0, PixelSize.Width, PixelSize.Height - height - barHeight));

                canvas.Restore();

                if (_font1 == null)
                {
                    using SKTypeface tf = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal);
                    _font1 = new SKFont(tf, height);
                    _font2 = new SKFont(tf, barHeight - 2);
                }

                if (_white == null)
                {
                    _white = new SKPaint();
                    _white.Color = SKColor.Parse("FFFFFF");
                }

                float size = _font1.MeasureText(_vttToken!.Name);

                canvas.DrawText(_vttToken!.Name, (_pixelSize.Width - size) / 2, _pixelSize.Height - _font1.Metrics.Descent, _font1, _white);

                int max = int.Parse(_vttToken.HitPointInfo?.Maximum?.ToString() ?? "0");
                int cur = _vttToken.HitPointInfo?.Current ?? 0;

                using SKPaint color = new SKPaint()
                {
                    Color = SKColor.Parse("1A6AFF")
                };

                float barWidth = _pixelSize.Width * (cur / (float)max);

                canvas.DrawRect(0, _pixelSize.Height - height - barHeight, barWidth, barHeight, color);
                string hp = $"{cur} / {max}";
                size = _font2!.MeasureText(hp);

                canvas.DrawText(hp, (barWidth - size) / 2, _pixelSize.Height - height - _font2.Metrics.Bottom, _font2, _white);

                _isDirty = false;

                base.Draw(canvas);
            }

            public override bool NeedDraw => _isDirty;

        }

    }
}
