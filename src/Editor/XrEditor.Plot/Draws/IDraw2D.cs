using SkiaSharp;
using XrMath;

namespace XrEditor.Plot
{
    public interface IDraw2D
    {
        void Draw(SKCanvas canvas, Rect2 rect);
    }
}
