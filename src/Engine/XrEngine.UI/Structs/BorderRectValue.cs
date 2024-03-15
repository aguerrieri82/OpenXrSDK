using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine.UI
{
    public struct BorderRectValue
    {
        public static BorderRectValue All(float width, Color color, BorderStyle style = BorderStyle.Solid, Unit unit = Unit.Dp)
        {
            return new BorderRectValue
            {
                Top = BorderValue.Get(width, color, style, unit),
                Left = BorderValue.Get(width, color, style, unit),
                Right = BorderValue.Get(width, color, style, unit),
                Bottom = BorderValue.Get(width, color, style, unit),
            };
        }

        public Color Color
        {
            set
            {
                Top.Color = value;
                Left.Color = value;
                Right.Color = value;
                Bottom.Color = value;
            }
        }

        public float Width
        {
            set
            {
                Top.Width = value;
                Left.Width = value;
                Right.Width = value;
                Bottom.Width = value;
            }
        }

        public BorderStyle Style
        {
            set
            {
                Top.Style = value;
                Left.Style = value;
                Right.Style = value;
                Bottom.Style = value;
            }
        }

        public static implicit operator BorderRectValue(float value)
        {
            if (value == 0)
                return All(0, Color.Transparent, BorderStyle.None);
            return All(value, Color.Black, BorderStyle.Solid);
        }


        public BorderValue Top;

        public BorderValue Left;

        public BorderValue Right;

        public BorderValue Bottom;
    }
}
