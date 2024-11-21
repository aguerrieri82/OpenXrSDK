using SkiaSharp;
using XrMath;

namespace XrEditor
{
    public interface IDraw2D
    {
        void Draw(SKCanvas canvas, Rect2 rect);
    }
}
