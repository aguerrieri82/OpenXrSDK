using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine.UI
{
    public enum BorderStyle
    {
        None,
        Solid
    }

    public struct BorderValue
    {
        public static BorderValue Get(float width, Color color, BorderStyle style = BorderStyle.Solid, Unit unit = Unit.Dp)
        {
            return new BorderValue
            {
                Color = color,
                Style = style,
                Width = UnitValue.Get(width, unit)
            };
        }

        public readonly bool HasValue => Style != BorderStyle.None && Width.Value > 0;

        public Color Color;

        public UnitValue Width;

        public BorderStyle Style;
    }
}
