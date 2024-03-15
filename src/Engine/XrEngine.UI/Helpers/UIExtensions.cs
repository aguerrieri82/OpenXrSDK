﻿using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using XrEngine.UI.Components;
using XrMath;

namespace XrEngine.UI
{
    public static class UIExtensions
    {
        public static Vector2 Position(this UiPointerEvent ev, UiComponent component)
        {
            return ev.ScreenPosition - component.ClientRect.Position;
        }

        public static void Focus(this UiComponent component)
        {
            UiFocusManager.SetFocus(component);
        }

        public static UiComponent? HitTest(this UiComponent component, Vector2 point)
        {
            UiComponent? Visit(UiComponent curItem)
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

            return Visit(component);
        }

        public static float ToPixel(this UiStyleValue<UnitValue> value, UiComponent ctx, float reference = 0)
        {
            return value.Value.ToPixel(ctx, reference);
        }

        public static float ToPixel(this UiStyleValue<UnitValue> value, UiComponent ctx, UiValueReference reference = UiValueReference.None)
        {
            return value.Value.ToPixel(ctx, reference);
        }

        public static SKFont GetFont(this UiStyle style)
        {
            return SKResources.Font(
                style.FontFamily.Value!,
                style.FontSize.ToPixel(style.Owner, UiValueReference.ParentFontSize)
            );
        }

        public static void SetPosition(this UiComponent comp, float x, float y, Unit unit = Unit.Dp)
        {
            comp.Style.Top = UnitValue.Get(y, unit);
            comp.Style.Left = UnitValue.Get(x, unit);
        }

        public static void SetSize(this UiComponent comp, float width, float height, Unit unit = Unit.Dp)
        {
            comp.Style.Width = UnitValue.Get(width, unit);
            comp.Style.Height = UnitValue.Get(height, unit);
        }

        public static void SetRect(this UiComponent comp, Rect2 rect)
        {
            comp.SetPosition(rect.X, rect.Y);
            comp.SetSize(rect.Width, rect.Height);
        }


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

        public static T AddChild<T>(this UiContainer self) where T : UiComponent, new()
        {
            var res = new T();
            self.AddChild(res);
            return res;
        }
    }
}
