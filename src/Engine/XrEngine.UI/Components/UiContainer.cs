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
            _layoutParams = null;
            base.InvalidateLayout();
        }

        protected override Size2 MeasureWork(Size2 availSize)
        {
            var manager = GetLayoutManager();

            _layoutParams = manager.ExtractLayoutParams(this);  

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
