using CanvasUI;
using XrMath;

namespace XrEngine.UI
{
    public static class UiExtensions
    {
        public static void SetRatio(this CanvasView3D canvas, float width, float ratio)
        {
            canvas.Size = new Size2(width, width / ratio);
        }

        public static void SetInches(this CanvasView3D canvas, float diagonal, float ratio)
        {
            var height = diagonal / MathF.Sqrt(ratio * ratio + 1);
            var width = ratio * height;

            canvas.Size = new Size2(width * UnitConv.InchesToMeter, height * UnitConv.InchesToMeter);
        }
    }
}
