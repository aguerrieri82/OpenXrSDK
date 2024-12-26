using SkiaSharp;
using XrMath;
using Color = XrMath.Color;

namespace XrEditor.Plot
{
    public class DrawPath : IDraw2D
    {
        public void Draw(SKCanvas canvas, Rect2 rect)
        {
            var func = Path!.ToFunctionY(0.1f, -1);

            var bounds = func.Bounds();

            var paint = new SKPaint();
            paint.Color = SKColor.Parse(Color.ToHex());
            paint.StrokeWidth = LineWidth;

            var t = 0f;

            while (t + Dt <= 1)
            {
                var x1 = bounds.Min.X + bounds.Size.X * t;
                var x2 = bounds.Min.X + bounds.Size.X * (t + Dt);
                var y1 = func.Value(x1);
                var y2 = func.Value(x2);
                canvas.DrawLine(new Vector2(x1, y1) - bounds.Min, new Vector2(x2, y2) - bounds.Min, paint);
                t += Dt;
            }
        }

        public float Dt;

        public Color Color;

        public float LineWidth;

        public Path2? Path;
    }
}
