using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrMath;

namespace XrEngine.UI
{
    public class UiPanel : UiComponent
    {
        protected List<UiComponent> _children = [];


        protected override Size2 MeasureWork(Size2 availSize)
        {
            foreach (var child in _children)
                child.Measure(availSize);

            return base.MeasureWork(availSize);
        }

        protected override void ArrangeWork(Rect2 finalRect)
        {
            var padding = Style.Padding.ActualValue(this);

            var newRect = finalRect;

            newRect.Left += padding.Left.ToPixel(this);
            newRect.Right -= padding.Right.ToPixel(this);

            newRect.Top += padding.Top.ToPixel(this);
            newRect.Bottom -= padding.Bottom.ToPixel(this);

            foreach (var child in _children)
                child.Arrange(newRect);

            base.ArrangeWork(finalRect);
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

        public IReadOnlyList<UiComponent> Children => _children;
    }
}
