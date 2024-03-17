using SkiaSharp;
using System.Numerics;
using XrMath;

namespace CanvasUI
{
    public static class UiExtensions
    {

        #region UI ELEMENT

        public static void SetPosition(this UiElement comp, float x, float y, Unit unit = Unit.Dp)
        {
            comp.Style.Top = UnitValue.Get(y, unit);
            comp.Style.Left = UnitValue.Get(x, unit);
        }

        public static void SetSize(this UiElement comp, float width, float height, Unit unit = Unit.Dp)
        {
            comp.Style.Width = UnitValue.Get(width, unit);
            comp.Style.Height = UnitValue.Get(height, unit);
        }

        public static void SetRect(this UiElement comp, Rect2 rect)
        {
            comp.SetPosition(rect.X, rect.Y);
            comp.SetSize(rect.Width, rect.Height);
        }


        public static IEnumerable<UiElement> VisualAncestorsAndSelf(this UiElement? self)
        {
            var curElement = self;

            while (curElement != null)
            {
                yield return curElement;
                curElement = curElement.VisualParent;
            }
        }

        public static void Focus(this UiElement self)
        {
            UiFocusManager.SetFocus(self);
        }

        public static UiElement? HitTest(this UiElement self, Vector2 point)
        {
            UiElement? Visit(UiElement curItem)
            {
                if (curItem.ActualStyle.Visibility == UiVisibility.Visible &&
                    curItem.ClientRect.Contains(point))
                {
                    foreach (var child in curItem.VisualChildren)
                    {
                        var childRes = Visit(child);
                        if (childRes != null)
                            return childRes;
                    }

                    return curItem;
                }

                return null;
            }

            return Visit(self);
        }

        #endregion

        #region STYLE

        public static float ToPixel(this UiStyleValue<UnitValue> self, UiElement ctx, float reference = 0)
        {
            return self.Value.ToPixel(ctx, reference);
        }

        public static float ToPixel(this UiStyleValue<UnitValue> self, UiElement ctx, UiValueReference reference = UiValueReference.None)
        {
            return self.Value.ToPixel(ctx, reference);
        }

        public static SKFont GetFont(this UiStyle style)
        {
            return SKResources.Font(
                style.FontFamily.Value!,
                style.FontSize.ToPixel(style.Owner, UiValueReference.ParentFontSize)
            );
        }

        #endregion

        #region MISC

        public static Vector2 Position(this UiPointerEvent ev, UiElement element)
        {
            return ev.ScreenPosition - element.ClientRect.Position;
        }
        public static T AddChild<T>(this UiContainer self) where T : UiElement, new()
        {
            var res = new T();
            self.AddChild(res);
            return res;
        }

        #endregion

    }
}
