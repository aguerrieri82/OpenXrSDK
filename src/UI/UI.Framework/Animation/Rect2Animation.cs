using XrMath;

namespace CanvasUI
{
    public class Rect2Animation : BaseAnimation<Rect2>
    {
        protected override Rect2 Interpolate(Rect2 from, Rect2 to, float t)
        {
            return new Rect2
            {
                X = from.X + (to.X - from.X) * t,
                Y = from.Y + (to.Y - from.Y) * t,
                Width = from.Width + (to.Width - from.Width) * t,
                Height = from.Height + (to.Height - from.Height) * t
            };
        }
    }
}
