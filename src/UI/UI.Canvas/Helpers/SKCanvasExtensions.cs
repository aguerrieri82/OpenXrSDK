using SkiaSharp;
using XrMath;

namespace CanvasUI
{
    public static class SKCanvasExtensions
    {
        public static SKRect ToSKRect(this Rect2 rect)
        {
            return new SKRect(rect.Left, rect.Top, rect.Right, rect.Bottom);
        }

        public static void DrawRect(this SKCanvas canvas, Rect2 rect, UiStyle style)
        {
            var bkColor = style.BackgroundColor.Value;
            if (bkColor != null)
            {
                var paint = SKResources.FillColor(bkColor.Value);
                canvas.DrawRect(rect.X, rect.Y, rect.Width, rect.Height, paint);
            }

            var border = style.Border.Value;
            if (border.Top.HasValue)
            {
                var paint = SKResources.Stroke(border.Top.Color, border.Top.Width.ToPixel(style.Owner, UiValueReference.ParentWidth));
                canvas.DrawLine(rect.X, rect.Y, rect.Right, rect.Y, paint);
            }
            if (border.Bottom.HasValue)
            {
                var paint = SKResources.Stroke(border.Bottom.Color, border.Bottom.Width.ToPixel(style.Owner, UiValueReference.ParentWidth));
                canvas.DrawLine(rect.X, rect.Bottom, rect.Right, rect.Bottom, paint);
            }
            if (border.Left.HasValue)
            {
                var paint = SKResources.Stroke(border.Left.Color, border.Left.Width.ToPixel(style.Owner, UiValueReference.ParentHeight));
                canvas.DrawLine(rect.X, rect.Y, rect.X, rect.Bottom, paint);
            }
            if (border.Right.HasValue)
            {
                var paint = SKResources.Stroke(border.Right.Color, border.Right.Width.ToPixel(style.Owner, UiValueReference.ParentHeight));
                canvas.DrawLine(rect.Right, rect.Y, rect.Right, rect.Bottom, paint);
            }
        }
    }
}
