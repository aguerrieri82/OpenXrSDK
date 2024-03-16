using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using XrMath;

namespace XrEngine.UI
{
    public class UiContainer : UiComponent
    {
        protected List<UiComponent> _children = [];
        protected object? _layoutParams;


        protected void UpdateLayoutParams()
        {
            var layout = ActualStyle.Layout.Value;

            if (layout == UiLayoutType.Flex)
            {
                var lp = new FlexLayoutManager.LayoutParams
                {
                    AlignContent = ActualStyle.AlignContent.Value,
                    AlignItems = ActualStyle.AlignItems.Value,
                    Gap = new Vector2(ActualStyle.ColGap.Value, ActualStyle.RowGap.Value),
                    JustifyContent = ActualStyle.JustifyContent.Value,
                    Orientation = ActualStyle.FlexDirection.Value,
                    WrapMode = ActualStyle.LayoutWrap.Value,
                    Children = new FlexLayoutManager.ChildParams[_children.Count]
                };

                for (var i = 0; i < _children.Count; i++)
                {
                    var child = _children[i];

                    lp.Children[i] = new FlexLayoutManager.ChildParams
                    {
                        Align = child.ActualStyle.AlignSelf.Value,
                        Basis = child.ActualStyle.FlexBasis.Value,
                        Grow = child.ActualStyle.FlexGrow.Value,
                        Shrink = child.ActualStyle.FlexShrink.Value,
                        Item = child
                    };
                }

                _layoutParams = lp;
            }
            else if (layout == UiLayoutType.Absolute)
            {
                var lp = new AbsoluteLayoutManager.LayoutParams
                {
                    Children = new AbsoluteLayoutManager.ChildParams[_children.Count]
                };

                for (var i = 0; i < _children.Count; i++)
                {
                    var child = _children[i];
                    lp.Children[i] = new AbsoluteLayoutManager.ChildParams
                    {
                        Position = new Vector2(child.ActualStyle.Left.ToPixel(child, UiValueReference.ParentWidth),
                                               child.ActualStyle.Top.ToPixel(child, UiValueReference.ParentHeight))
                    };
                }
                _layoutParams = lp;
            }
            else
                _layoutParams = null;
        }

        protected IUiLayoutManager GetLayoutManager()
        {
            var layout = ActualStyle.Layout.Value;

            if (layout == UiLayoutType.Flex)
                return FlexLayoutManager.Instance; 
            
            if (layout == UiLayoutType.Absolute)
                return AbsoluteLayoutManager.Instance;

            throw new NotSupportedException();
        }

        protected override void InvalidateLayout()
        {
            base.InvalidateLayout();
        }


        protected override Size2 MeasureWork(Size2 availSize)
        {
            UpdateLayoutParams();

            var manager = GetLayoutManager();

            return manager.Measure(availSize, _layoutParams);
        }


        protected override Size2 ArrangeWork(Rect2 finalRect)
        {
            var manager = GetLayoutManager();

            return manager.Arrange(finalRect, _layoutParams);
        }

        protected override void DrawWork(SKCanvas canvas)
        {
            foreach (var child in _children)
                child.Draw(canvas);
        }

        public void AddChild(UiComponent child)
        {
            if (child.Parent == this)
                return;

            child.Parent?.RemoveChild(child);  
            
            _children.Add(child);

            _isDirty = true;
            _isLayoutDirty = true;

            child.Parent = this;
        }

        public void RemoveChild(UiComponent child) 
        {
            _children.Remove(child);

            if (child.Parent == this)
                child.Parent = null;

            _isDirty = true;
            _isLayoutDirty = true;
        }

        public void Clear()
        {
            foreach (var child in _children)
                child.Parent = null;

            _children.Clear();

            _isDirty = true;
            _isLayoutDirty = true;
        }


        public override IEnumerable<UiComponent> VisualChildren => _children;

        public IReadOnlyList<UiComponent> Children => _children;
    }
}
