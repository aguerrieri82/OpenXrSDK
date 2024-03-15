using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using XrMath;

namespace XrEngine.UI
{
 
    public struct FlexLayoutChildParams
    {
        public float Basis;

        public float Grow;

        public float Shrink;

        public UiAlignment? Align;

        public ILayoutItem Item;
    }

    public struct FlexLayoutParams
    {
        public UIOrientation Orientation;

        public UiAlignment AlignItems;

        public UiAlignment AlignContent;

        public UiAlignment JustifyContent;

        public Vector2 Gap;

        public UiWrapMode WrapMode;

        public FlexLayoutChildParams[] Children;
    }


    public class FlexLayoutManager : IUiLayoutManager
    {
        struct MeasureResult
        {
            public Size2[][] Items;

            public Size2 FinalSize;

            public float Basis;
           
            public float Shrink;

            public float Grow;
        }

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

        protected FlexLayoutManager() { }

        MeasureResult MeasureWork(Size2 availSize, FlexLayoutParams lp, bool isArrange)
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
                if (curRow.Count > 0)
                    items.Add(curRow);

                else if (items.Count > 0)
                    curSize.Height -= gap.Y;

                curRowSize.Width -= gap.X;

                var occupyHeight = curRowSize.Height + gap.Y;

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
                result.Shrink += child.Basis;
                result.Grow += child.Basis;
            }

            foreach (var child in lp.Children)
            {
                var childAvailSize = leftRowSize;

                if (child.Basis != 0)
                    childAvailSize.Width = availSize.Width * child.Basis / result.Basis;

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

                var occupyWidth = childSize.Width + gap.X;

                curRowSize.Width += occupyWidth;
                curRowSize.Height = Math.Max(curRowSize.Height, childSize.Height);

                leftRowSize.Width -= occupyWidth;

                curRow.Add(childSize);
            }

            NewRow(true);

            if (
                (result.Shrink > 0 && curSize.Width > availSize.Width) ||
                (curSize.Width < availSize.Width && (
                    result.Grow > 0 ||
                    lp.JustifyContent == UiAlignment.SpaceBetween ||
                    lp.JustifyContent == UiAlignment.SpaceAround)))

                curSize.Width = availSize.Width;

            result.FinalSize = curSize;

            result.Items = items.Select(a => a.ToArray()).ToArray();

            return result;
        }

        public Size2 Measure(Size2 availSize, FlexLayoutParams lp)
        {
            var result = MeasureWork(availSize, lp, false);
            return result.FinalSize;
        }

        public void Arrange(Rect2 finalRect, FlexLayoutParams lp)
        {
            var measure = MeasureWork(finalRect.Size, lp, true);

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

                else if (lp.AlignContent == UiAlignment.SpaceBetween)
                    gap.Y += overflow.X / measure.Items.Length - 1;

                else if (lp.AlignContent == UiAlignment.SpaceAround)
                {
                    gap.Y = overflow.X / measure.Items.Length + 1;
                    curPos.Y += gap.X;
                }
            }

            //Process rows
            foreach (var row in measure.Items)
            {

                var rowSize = new Size2
                {
                    Width = row.Sum(a => a.Width),
                    Height = row.Max(a => a.Height),
                };

                var rowShrink = lp.Children.Skip(childIndex).Take(row.Length).Sum(a => a.Shrink);
                var rowGrow = lp.Children.Skip(childIndex).Take(row.Length).Sum(a => a.Grow);

                overflow.X = finalRect.Width - (rowSize.Width + gap.X * (row.Length -1));
                
                curPos.X = finalRect.X;

                if (overflow.X > 0 && rowShrink == 0 && rowGrow == 0)
                {
                    if (lp.JustifyContent == UiAlignment.Center)
                        curPos.X += overflow.X / 2;

                    else if (lp.JustifyContent == UiAlignment.End)
                        curPos.X += overflow.X;

                    else if (lp.JustifyContent == UiAlignment.SpaceBetween)
                        gap.X = overflow.X / row.Length - 1;

                    else if (lp.JustifyContent == UiAlignment.SpaceAround)
                    {
                        gap.X = overflow.X / row.Length + 1;
                        curPos.X += gap.X;
                    }
                }

                for (var i = 0; i < row.Length; i++)
                {
                    ref var childSize = ref row[i];
                    var child = lp.Children[childIndex];

                    if (rowShrink > 0 && overflow.X < 0)
                        childSize.Width += overflow.X * (child.Shrink / rowShrink);
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
                        childRect.Height = rowSize.Height;

                    else if (align == UiAlignment.End)
                        childRect.Y += rowSize.Height - childSize.Height;

                    else if (align == UiAlignment.Center)
                        childRect.Y += (rowSize.Height - childSize.Height) / 2;

                    var newSize = child.Item.Arrange(Convert(childRect, lp.Orientation));

                    curPos.X += childRect.Width + gap.X;

                    childIndex++;
                }

                curPos.X -= gap.X;

                //Debug.Assert(curPos.X == rowSize.Width);

                curPos.Y += rowSize.Height + gap.Y;
            }

            curPos.Y -= gap.Y;

            //Debug.Assert(curPos.Y == measure.FinalSize.Height);
        }

        Size2 IUiLayoutManager.Measure(Size2 availSize, object? layoutParams)
        {
            return Measure(availSize, (FlexLayoutParams)layoutParams!);
        }

        Size2 IUiLayoutManager.Arrange(Rect2 finalRect, object? layoutParams)
        {
            Arrange(finalRect, (FlexLayoutParams)layoutParams!);

            return finalRect.Size;
        }

        public static readonly FlexLayoutManager Instance = new FlexLayoutManager();
    }
}
