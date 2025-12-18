using SkiaSharp;
using XrMath;

namespace CanvasUI
{
    public class UiContainer : UiElement
    {
        protected List<UiElement> _children = [];
        protected object? _layoutParams;

        protected IUiLayoutManager GetLayoutManager()
        {
            UiLayoutType layout = ActualStyle.Layout.Value;

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
            IUiLayoutManager manager = GetLayoutManager();

            _layoutParams = manager.ExtractLayoutParams(this);

            return manager.Measure(availSize, _layoutParams);
        }

        protected override Size2 ArrangeWork(Rect2 finalRect)
        {
            IUiLayoutManager manager = GetLayoutManager();

            return manager.Arrange(finalRect, _layoutParams);
        }

        protected override void DrawWork(SKCanvas canvas)
        {
            foreach (UiElement child in _children)
                child.Draw(canvas);
        }

        public void AddChild(UiElement child)
        {
            if (child.Parent == this)
                return;

            child.Parent?.RemoveChild(child);

            _children.Add(child);

            _isDirty = true;
            _isLayoutDirty = true;

            child.Parent = this;
        }

        public void RemoveChild(UiElement child)
        {
            _children.Remove(child);

            if (child.Parent == this)
                child.Parent = null;

            _isDirty = true;
            _isLayoutDirty = true;
        }

        public void Clear()
        {
            foreach (UiElement child in _children)
                child.Parent = null;

            _children.Clear();

            _isDirty = true;
            _isLayoutDirty = true;
        }

        public override void Dispose()
        {
            for (int i = _children.Count - 1; i >= 0; i--)
            {
                UiElement child = _children[i];
                _children.RemoveAt(i);
                child.Dispose();
            }

            base.Dispose();
        }

        public override IEnumerable<UiElement> VisualChildren => _children;

        public IReadOnlyList<UiElement> Children => _children;
    }
}
