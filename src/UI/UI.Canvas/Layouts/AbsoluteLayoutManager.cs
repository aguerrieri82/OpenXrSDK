using System.Numerics;
using XrMath;

namespace CanvasUI
{
    public class AbsoluteLayoutManager : IUiLayoutManager
    {
        #region STRUCT

        public struct ChildParams
        {
            public Vector2 Position;

            public IUiLayoutItem Item;
        }

        public struct LayoutParams
        {
            public ChildParams[] Children;
        }

        #endregion

        protected AbsoluteLayoutManager() { }

        public Size2 Arrange(Rect2 finalRect, object? layoutParams)
        {
            LayoutParams lp = (LayoutParams)layoutParams!;

            Size2 totSize = new Size2();

            foreach (ChildParams child in lp.Children)
            {
                Rect2 childRect = new Rect2
                {
                    X = child.Position.X,
                    Y = child.Position.Y,
                    Width = child.Item.DesiredSize.Width,
                    Height = child.Item.DesiredSize.Height
                };

                child.Item.Arrange(childRect);

                totSize.Width = MathF.Max(totSize.Width, childRect.Right);
                totSize.Height = MathF.Max(totSize.Height, childRect.Bottom);
            }

            return totSize;
        }

        public Size2 Measure(Size2 availSize, object? layoutParams)
        {
            LayoutParams lp = (LayoutParams)layoutParams!;

            Size2 totSize = new Size2();

            foreach (ChildParams child in lp.Children)
            {
                Size2 childSize = availSize;

                childSize.Width -= child.Position.X;
                childSize.Height -= child.Position.Y;

                childSize = child.Item.Measure(childSize);

                totSize.Width = MathF.Max(totSize.Width, childSize.Width + child.Position.X);
                totSize.Height = MathF.Max(totSize.Height, childSize.Height + child.Position.Y);
            }

            return totSize;
        }

        public object? ExtractLayoutParams(UiContainer container)
        {
            LayoutParams result = new LayoutParams
            {
                Children = new ChildParams[container.Children.Count]
            };

            for (int i = 0; i < container.Children.Count; i++)
            {
                UiElement child = container.Children[i];
                result.Children[i] = new ChildParams
                {
                    Position = new Vector2(child.ActualStyle.Left.ToPixel(child, UiValueReference.ParentWidth),
                                           child.ActualStyle.Top.ToPixel(child, UiValueReference.ParentHeight))
                };
            }

            return result;
        }

        public static readonly AbsoluteLayoutManager Instance = new();
    }
}
