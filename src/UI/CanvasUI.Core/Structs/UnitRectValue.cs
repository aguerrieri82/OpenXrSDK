

namespace CanvasUI
{
    public struct UnitRectValue
    {
        public static UnitRectValue Set(float top = 0, float left = 0, float bottom = 0, float right = 0, Unit unit = Unit.Dp)
        {
            return new UnitRectValue
            {
                Top = UnitValue.Get(top, unit),
                Left = UnitValue.Get(left, unit),
                Bottom = UnitValue.Get(bottom, unit),
                Right = UnitValue.Get(right, unit),

            };
        }

        public static UnitRectValue All(float value, Unit unit = Unit.Dp)
        {
            return Set(value, value, value, value, unit);
        }

        public static UnitRectValue Axis(float hValue, float vValue, Unit unit = Unit.Dp)
        {
            return Set(vValue, hValue, vValue, hValue, unit);
        }

        public float ToHorizontalPixel(UiElement ctx, float reference = 0)
        {
            return Left.ToPixel(ctx, reference) + Right.ToPixel(ctx, reference);
        }

        public float ToVerticalPixel(UiElement ctx, float reference = 0)
        {
            return Top.ToPixel(ctx, reference) + Bottom.ToPixel(ctx, reference);
        }

        public float ToHorizontalPixel(UiElement ctx, UiValueReference reference = UiValueReference.None)
        {
            return Left.ToPixel(ctx, reference) + Right.ToPixel(ctx, reference);
        }

        public float ToVerticalPixel(UiElement ctx, UiValueReference reference = UiValueReference.None)
        {
            return Top.ToPixel(ctx, reference) + Bottom.ToPixel(ctx, reference);
        }


        public static implicit operator UnitRectValue(float value)
        {
            return All(value);
        }


        public float Value
        {
            set
            {
                Top.Value = value;
                Left.Value = value;
                Right.Value = value;
                Bottom.Value = value;
            }
        }

        public UnitValue Top;
        
        public UnitValue Left;
        
        public UnitValue Right;

        public UnitValue Bottom;
    }
}
