using System.Globalization;
using System.Text;
using XrEngine.UI.Components;

namespace XrEngine.UI
{
    public enum Unit
    {
        Dp,
        Em,
        Perc
    }

    public struct UnitValue
    {
        public UnitValue() { }

        public UnitValue(float value, Unit unit = Unit.Dp)
        {
            Value = value;
            Unit = unit;
        }

        public UnitValue(string value)
        {
            var unitIndex = -1;

            for (var i = 0; i < value.Length; i++)
            {
                if (!char.IsNumber(value[i]))
                {
                    unitIndex = i;
                    break;
                }    
            }

            if (unitIndex != -1)
            {
                var unit = value.AsSpan(unitIndex);
                Unit = unit switch
                {
                    "dp" => Unit.Dp,
                    "%" => Unit.Perc,
                    "em" => Unit.Em,
                    _ => throw new NotSupportedException()
                };
                Value = float.Parse(value.AsSpan(0, unitIndex), CultureInfo.InvariantCulture);
            }
            else
                Value = float.Parse(value, CultureInfo.InvariantCulture);
        }

        public readonly float ToPixel(UiComponent ctx, float reference = 0)
        {
            if (Unit == Unit.Dp)
                return Value;

            if (Unit == Unit.Em)
                return ctx.ActualStyle.FontSize.ToPixel(ctx) * Value;

            if (Unit == Unit.Perc)
                return reference * Value / 100f;

            throw new NotSupportedException();
        }

        public static UnitValue Get(float value, Unit unit = Unit.Dp)
        {
            return new UnitValue(value, unit);
        }

        public static UnitValue Dp(float value)
        {
            return new UnitValue(value, Unit.Dp);
        }

        public static UnitValue Perc(float value)
        {
            return new UnitValue(value, Unit.Perc);
        }

        public static implicit operator UnitValue(float value)
        {
            return new UnitValue(value);
        }


        public static implicit operator UnitValue(string value)
        {
            return new UnitValue(value);
        }

        public static implicit operator float(UnitValue value)
        {
            return value.Value;
        }

        public float Value;

        public Unit Unit;

    }
}
