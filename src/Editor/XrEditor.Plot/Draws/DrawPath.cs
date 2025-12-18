using SkiaSharp;
using System.Numerics;
using XrMath;
using Color = XrMath.Color;

namespace XrEditor.Plot
{
    public class DrawPath : IDraw2D
    {
        public void Draw(SKCanvas canvas, Rect2 rect)
        {
            DiscreteFunction func = Path!.ToFunctionY(0.1f, -1);

            Bounds2 bounds = func.Bounds();

            SKPaint paint = new SKPaint();
            paint.Color = SKColor.Parse(Color.ToHex());
            paint.StrokeWidth = LineWidth;

            float t = 0f;

            while (t + Dt <= 1)
            {
                float x1 = bounds.Min.X + bounds.Size.X * t;
                float x2 = bounds.Min.X + bounds.Size.X * (t + Dt);
                float y1 = func.Value(x1);
                float y2 = func.Value(x2);
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
