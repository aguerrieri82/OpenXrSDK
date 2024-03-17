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

            public ILayoutItem Item;
        }

        public struct LayoutParams
        {
            public ChildParams[] Children;
        }

        #endregion

        protected AbsoluteLayoutManager() { }

        public Size2 Arrange(Rect2 finalRect, object? layoutParams)
        {
            var lp = (LayoutParams)layoutParams!;

            var totSize = new Size2();

            foreach (var child in lp.Children)
            {
                var childRect = new Rect2
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
            var lp = (LayoutParams)layoutParams!;

            var totSize = new Size2();

            foreach (var child in lp.Children)
            {
                var childSize = availSize;

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
            var result = new LayoutParams
            {
                Children = new ChildParams[container.Children.Count]
            };

            for (var i = 0; i < container.Children.Count; i++)
            {
                var child = container.Children[i];
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
