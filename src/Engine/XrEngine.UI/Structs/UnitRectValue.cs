using XrEngine.UI.Components;

namespace XrEngine.UI
{
    public struct UnitRectValue
    {
        public static UnitRectValue All(float value, Unit unit = Unit.Dp)
        {
            return new UnitRectValue
            {
                Top = UnitValue.Get(value, unit),
                Left = UnitValue.Get(value, unit),
                Right = UnitValue.Get(value, unit),
                Bottom = UnitValue.Get(value, unit),
            };
        }

        public static UnitRectValue Axis(float hValue, float vValue, Unit unit = Unit.Dp)
        {
            return new UnitRectValue
            {
                Top = UnitValue.Get(vValue, unit),
                Left = UnitValue.Get(hValue, unit),
                Right = UnitValue.Get(hValue, unit),
                Bottom = UnitValue.Get(vValue, unit),
            };
        }

        public float ToHorizontalPixel(UiComponent ctx)
        {
            return Left.ToPixel(ctx) + Right.ToPixel(ctx);
        }

        public float ToVerticalPixel(UiComponent ctx)
        {
            return Top.ToPixel(ctx) + Bottom.ToPixel(ctx);
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

        public static implicit operator UnitRectValue(float value)
        {
            return All(value);
        }


        public UnitValue Top;
        
        public UnitValue Left;
        
        public UnitValue Right;

        public UnitValue Bottom;
    }
}
