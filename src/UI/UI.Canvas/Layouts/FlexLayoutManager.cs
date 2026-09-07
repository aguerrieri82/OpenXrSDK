using System.Numerics;
using XrMath;

namespace CanvasUI
{

    public class FlexLayoutManager : IUiLayoutManager
    {
        #region STRUCTS 

        public struct ChildParams
        {
            public float Basis;

            public float Grow;

            public float Shrink;

            public UiAlignment? Align;

            public IUiLayoutItem Item;
        }

        public struct LayoutParams
        {
            public UIOrientation Orientation;

            public UiAlignment AlignItems;

            public UiAlignment AlignContent;

            public UiAlignment JustifyContent;

            public Vector2 Gap;

            public UiWrapMode WrapMode;

            public string? Name;

            public ChildParams[] Children;
        }

        struct MeasureResult
        {
            public Size2[][] Items;

            public Size2 FinalSize;

            public float Basis;

            public float Shrink;

            public float Grow;
        }

        #endregion

        protected FlexLayoutManager() { }

        static Size2 Convert(Size2 value, UIOrientation ort)
        {
            if (ort == UIOrientation.Vertical)
                return new Size2 { Width = value.Height, Height = value.Width };
            return value;
        }

        static Vector2 Convert(Vector2 value, UIOrientation ort)
        {
            if (ort == UIOrientation.Vertical)
                return new Vector2 { X = value.Y, Y = value.X };
            return value;
        }

        static Rect2 Convert(Rect2 value, UIOrientation ort)
        {
            if (ort == UIOrientation.Vertical)
                return new Rect2 { X = value.Y, Y = value.X, Width = value.Height, Height = value.Width };
            return value;
        }

        static MeasureResult MeasureWork(Size2 availSize, LayoutParams lp, bool isArrange)
        {
            availSize = Convert(availSize, lp.Orientation);

            var curRowSize = Size2.Zero;
            var curSize = Size2.Zero;
            var leftRowSize = availSize;

            var result = new MeasureResult();
            var items = new List<List<Size2>>();

            var gap = Convert(lp.Gap, lp.Orientation);

            var curRow = new List<Size2>();

            void NewRow(bool lastRow)
            {
                if (curRow.Count == 0)
                    return;

                items.Add(curRow);

                curRowSize.Width -= gap.X;

                var occupyHeight = curRowSize.Height;

                if (!lastRow)
                    occupyHeight += gap.Y;

                curSize.Width = Math.Max(curRowSize.Width, curSize.Width);
                curSize.Height += occupyHeight;

                if (!lastRow)
                {
                    leftRowSize.Height -= occupyHeight;
                    leftRowSize.Width = availSize.Width;

                    curRowSize.Width = 0;
                    curRowSize.Height = 0;

                    curRow = [];
                }
            }

            foreach (var child in lp.Children)
            {
                result.Basis += child.Basis;
                result.Shrink += child.Shrink;
                result.Grow += child.Grow;
            }

            var availWidthWithGap = availSize.Width - gap.X * (lp.Children.Length - 1);

            foreach (var child in lp.Children)
            {
                var childAvailSize = leftRowSize;

                if (child.Basis != 0)
                    childAvailSize.Width = availWidthWithGap * child.Basis / result.Basis;

                var childSize = Convert(isArrange ?
                        child.Item.DesiredSize :
                        child.Item.Measure(Convert(childAvailSize, lp.Orientation)), lp.Orientation);

                if (childSize.Width > leftRowSize.Width &&
                    result.Shrink == 0 &&
                    curRow.Count > 0 &&
                    lp.WrapMode == UiWrapMode.Wrap)
                {
                    NewRow(false);
                }

                if (child.Basis != 0 && childSize.Width < childAvailSize.Width)
                    childSize.Width = childAvailSize.Width;

                var occupyWidth = childSize.Width + gap.X;

                curRowSize.Width += occupyWidth;
                curRowSize.Height = Math.Max(curRowSize.Height, childSize.Height);

                leftRowSize.Width -= occupyWidth;

                curRow.Add(childSize);
            }

            NewRow(true);

            if (items.Count > 0 && (
                (result.Shrink > 0 && curSize.Width > availSize.Width) ||
                (curSize.Width < availSize.Width && (
                    result.Grow > 0 ||
                    lp.JustifyContent == UiAlignment.SpaceBetween ||
                    lp.JustifyContent == UiAlignment.SpaceAround))))

                curSize.Width = availSize.Width;

            result.FinalSize = curSize;

            result.Items = items.Select(a => a.ToArray()).ToArray();

            return result;
        }

        static Size2 Measure(Size2 availSize, LayoutParams lp)
        {
            var result = MeasureWork(availSize, lp, false);
            return Convert(result.FinalSize, lp.Orientation);
        }

        static void Arrange(Rect2 finalRect, LayoutParams lp)
        {
            var measure = MeasureWork(finalRect.Size, lp, true);

            if (measure.Items.Length == 0)
                return;

            finalRect = Convert(finalRect, lp.Orientation);

            var childIndex = 0;

            var curPos = finalRect.Position;

            var overflow = new Vector2();

            var gap = Convert(lp.Gap, lp.Orientation);

            overflow.Y = finalRect.Height - measure.FinalSize.Height;

            if (overflow.Y > 0)
            {
                if (lp.AlignContent == UiAlignment.Center)
                    curPos.Y += overflow.Y / 2;

                else if (lp.AlignContent == UiAlignment.End)
                    curPos.Y += overflow.Y;

                else if (lp.AlignContent == UiAlignment.SpaceBetween && measure.Items.Length > 1)
                    gap.Y += overflow.Y / (measure.Items.Length - 1);

                else if (lp.AlignContent == UiAlignment.SpaceAround)
                {
                    var space = overflow.Y / measure.Items.Length;
                    gap.Y += space;
                    curPos.Y += space / 2;
                }
                else if (lp.AlignContent == UiAlignment.Stretch)
                    throw new NotSupportedException();
            }

            //Process rows
            foreach (var row in measure.Items)
            {
                var rowGap = gap.X;

                var rowSize = new Size2
                {
                    Width = row.Sum(a => a.Width),
                    Height = row.Max(a => a.Height),
                };

                var rowShrink = lp.Children.Skip(childIndex).Take(row.Length).Sum(a => a.Shrink);
                var rowGrow = lp.Children.Skip(childIndex).Take(row.Length).Sum(a => a.Grow);

                overflow.X = finalRect.Width - (rowSize.Width + rowGap * (row.Length - 1));

                curPos.X = finalRect.X;

                if (overflow.X > 0 && rowGrow == 0)
                {
                    if (lp.JustifyContent == UiAlignment.Center)
                        curPos.X += overflow.X / 2;

                    else if (lp.JustifyContent == UiAlignment.End)
                        curPos.X += overflow.X;

                    else if (lp.JustifyContent == UiAlignment.SpaceBetween && row.Length > 1)
                        rowGap += overflow.X / (row.Length - 1);

                    else if (lp.JustifyContent == UiAlignment.SpaceAround)
                    {
                        var space = overflow.X / row.Length;
                        rowGap += space;
                        curPos.X += space / 2;
                    }
                    else if (lp.JustifyContent == UiAlignment.Stretch)
                        throw new NotSupportedException();
                }

                if (rowShrink > 0 && overflow.X < 0)
                    ShrinkRow(row, lp.Children, childIndex, -overflow.X);

                for (var i = 0; i < row.Length; i++)
                {
                    ref var childSize = ref row[i];
                    var child = lp.Children[childIndex];

                    if (rowGrow > 0 && overflow.X > 0)
                        childSize.Width += overflow.X * (child.Grow / rowGrow);

                    var align = child.Align ?? lp.AlignItems;

                    var childRect = new Rect2
                    {
                        X = curPos.X,
                        Y = curPos.Y,
                        Width = childSize.Width,
                        Height = childSize.Height
                    };

                    if (align == UiAlignment.Stretch)
                        childRect.Height = lp.WrapMode == UiWrapMode.NoWrap ? finalRect.Height : rowSize.Height;

                    else if (align == UiAlignment.End)
                        childRect.Y += rowSize.Height - childSize.Height;

                    else if (align == UiAlignment.Center)
                        childRect.Y += (rowSize.Height - childSize.Height) / 2;

                    var newSize = child.Item.Arrange(Convert(childRect, lp.Orientation));

                    curPos.X += childRect.Width + rowGap;

                    childIndex++;
                }

                curPos.X -= rowGap;

                //Debug.Assert(curPos.X == rowSize.Width);

                curPos.Y += rowSize.Height + gap.Y;
            }

            curPos.Y -= gap.Y;

            //Debug.Assert(curPos.Y == measure.FinalSize.Height);
        }

        static void ShrinkRow(Size2[] row, ChildParams[] children, int childIndex, float overflow)
        {
            while (overflow > 0)
            {
                var shrink = 0f;

                for (var i = 0; i < row.Length; i++)
                {
                    if (row[i].Width > 0)
                        shrink += children[childIndex + i].Shrink;
                }

                if (shrink <= 0)
                    return;

                var removed = 0f;

                for (var i = 0; i < row.Length; i++)
                {
                    var factor = children[childIndex + i].Shrink;

                    if (row[i].Width > 0 && factor > 0 && row[i].Width <= overflow * factor / shrink)
                    {
                        removed += row[i].Width;
                        row[i].Width = 0;
                    }
                }

                if (removed > 0)
                {
                    overflow -= removed;
                    continue;
                }

                for (var i = 0; i < row.Length; i++)
                {
                    if (row[i].Width > 0)
                        row[i].Width = Math.Max(0, row[i].Width - overflow * children[childIndex + i].Shrink / shrink);
                }

                return;
            }
        }

        Size2 IUiLayoutManager.Measure(Size2 availSize, object? layoutParams)
        {
            return Measure(availSize, (LayoutParams)layoutParams!);
        }

        Size2 IUiLayoutManager.Arrange(Rect2 finalRect, object? layoutParams)
        {
            Arrange(finalRect, (LayoutParams)layoutParams!);

            return finalRect.Size;
        }

        public object? ExtractLayoutParams(UiContainer container)
        {
            var children = container.Children.Where(a => a.ActualStyle.Visibility != UiVisibility.Collapsed).ToArray();

            var result = new LayoutParams
            {
                AlignContent = container.ActualStyle.AlignContent.Value,
                AlignItems = container.ActualStyle.AlignItems.Value,
                Gap = new Vector2(container.ActualStyle.ColGap.Value, container.ActualStyle.RowGap.Value),
                JustifyContent = container.ActualStyle.JustifyContent.Value,
                Orientation = container.ActualStyle.FlexDirection.Value,
                WrapMode = container.ActualStyle.LayoutWrap.Value,
                Children = new ChildParams[children.Length],
                Name = container.Name
            };

            for (var i = 0; i < children.Length; i++)
            {
                var child = children[i];

                result.Children[i] = new ChildParams
                {
                    Align = child.ActualStyle.AlignSelf.Value,
                    Basis = child.ActualStyle.FlexBasis.Value,
                    Grow = child.ActualStyle.FlexGrow.Value,
                    Shrink = child.ActualStyle.FlexShrink.Value,
                    Item = child
                };
            }

            return result;
        }

        public static readonly FlexLayoutManager Instance = new();
    }
}
