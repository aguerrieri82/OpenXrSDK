using XrEngine.UI.Components;

namespace XrEngine.UI
{
    public enum Unit
    {
        Dp,
    }

    public struct UnitValue
    {
        public readonly float ToPixel(UiObject ctx)
        {
            if (Unit == Unit.Dp)
                return Value;
            throw new NotSupportedException();
        }

        public static UnitValue Get(float value, Unit unit = Unit.Dp)
        {
            return new UnitValue() { Value = value, Unit = unit };
        }


        public static UnitValue Dp(float value)
        {
            return new UnitValue() { Value = value, Unit = Unit.Dp };
        }

        public static implicit operator UnitValue(float value)
        {
            return new UnitValue() { Value = value };
        }

        public static implicit operator float(UnitValue value)
        {
            return value.Value;
        }

        public float Value;

        public Unit Unit;

    }
}
