using XrMath;

namespace CanvasUI
{
    public struct UiStyleBuilder
    {
        public UiStyleBuilder(UiStyle style)
        {
            Style = style;
        }

        public readonly UiStyleBuilder BackgroundColor(Color color)
        {
            Style.BackgroundColor = color;
            return this;
        }

        public readonly UiStyleBuilder Color(Color color)
        {
            Style.Color = color;
            return this;
        }


        public readonly UiStyleBuilder Padding(float value, Unit unit = Unit.Dp)
        {
            Style.Padding = UnitRectValue.All(value, unit);
            return this;
        }

        public readonly UiStyleBuilder Padding(float vert, float hor, Unit unit = Unit.Dp)
        {
            Style.Padding = UnitRectValue.Axis(vert, hor, unit);
            return this;
        }

        public readonly UiStyleBuilder Margin(float value, Unit unit = Unit.Dp)
        {
            Style.Margin = UnitRectValue.All(value, unit);
            return this;
        }

        public readonly UiStyleBuilder Margin(float vert, float hor, Unit unit = Unit.Dp)
        {
            Style.Margin = UnitRectValue.Axis(vert, hor, unit);
            return this;
        }

        public readonly UiStyleBuilder Border(float width, Color color, BorderStyle style = BorderStyle.Solid)
        {
            Style.Border = BorderRectValue.All(width, color, style);
            return this;
        }


        public readonly UiStyleBuilder AlignContent(UiAlignment value)
        {
            Style.AlignContent = value;
            return this;
        }

        public readonly UiStyleBuilder AlignItems(UiAlignment value)
        {
            Style.AlignItems = value;
            return this;
        }

        public readonly UiStyleBuilder FlexVertical()
        {
            Style.Layout = UiLayoutType.Flex;
            Style.FlexDirection = UIOrientation.Vertical;
            return this;
        }

        public readonly UiStyleBuilder FlexHorizontal()
        {
            Style.Layout = UiLayoutType.Flex;
            Style.FlexDirection = UIOrientation.Horizontal;
            return this;
        }

        public readonly UiStyleBuilder RowGap(float value, Unit unit = Unit.Dp)
        {
            Style.RowGap = UnitValue.Get(value, unit);
            return this;
        }

        public readonly UiStyleBuilder ColGap(float value, Unit unit = Unit.Dp)
        {
            Style.ColGap = UnitValue.Get(value, unit);
            return this;
        }

        public readonly UiStyleBuilder FlexShrink(float value)
        {
            Style.FlexShrink = value;
            return this;
        }

        public readonly UiStyleBuilder FlexGrow(float value)
        {
            Style.FlexGrow = value;
            return this;
        }

        public readonly UiStyleBuilder FlexBasis(float value)
        {
            Style.FlexBasis = value;
            return this;
        }

        public readonly UiStyleBuilder TextAlign(UiAlignment value)
        {
            Style.TextAlign = value;
            return this;
        }

        public readonly UiStyleBuilder TextAlignCenter()
        {
            Style.TextAlign = UiAlignment.Center;
            return this;
        }

        public readonly UiStyleBuilder TextAlignEnd()
        {
            Style.TextAlign = UiAlignment.End;
            return this;
        }

        public readonly UiStyleBuilder AlignSelf(UiAlignment value)
        {
            Style.AlignSelf = value;
            return this;
        }

        public readonly UiStyleBuilder Height(float value, Unit unit = Unit.Dp)
        {
            Style.Height = UnitValue.Get(value, unit);
            return this;
        }

        public readonly UiStyleBuilder Width(float value, Unit unit = Unit.Dp)
        {
            Style.Width = UnitValue.Get(value, unit);
            return this;
        }

        public readonly UiStyleBuilder Overflow(UiOverflow value)
        {
            Style.OverflowX = value;
            Style.OverflowY = value;
            return this;
        }


        public readonly UiStyleBuilder FontSize(float value, Unit unit = Unit.Dp)
        {
            Style.FontSize = UnitValue.Get(value, unit);
            return this;
        }


        public UiStyle Style;
    }
}
