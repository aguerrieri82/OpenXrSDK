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


        protected (IUiLayoutManager, object) GetLayoutManager()
        {
            var layout = ActualStyle.Layout.Value;

            if (layout == UiLayoutType.Flex)
            {
                var manager = FlexLayoutManager.Instance;

                var lp = new FlexLayoutParams
                {
                    AlignContent = ActualStyle.AlignContent.Value,
                    AlignItems = ActualStyle.AlignItems.Value,
                    Gap = new Vector2(ActualStyle.ColGap.Value, ActualStyle.RowGap.Value),
                    JustifyContent = ActualStyle.JustifyContent.Value,
                    Orientation = ActualStyle.FlexDirection.Value,
                    WrapMode = ActualStyle.LayoutWrap.Value,
                    Children = new FlexLayoutChildParams[_children.Count]
                };

                for (var i = 0; i < _children.Count; i++)
                {
                    var child = _children[i];

                    lp.Children[i] = new FlexLayoutChildParams
                    {
                        Align = child.ActualStyle.AlignSelf.Value,
                        Basis = child.ActualStyle.FlexBasis.Value,
                        Grow = child.ActualStyle.FlexGrow.Value,
                        Shrink = child.ActualStyle.FlexShrink.Value,
                        Item = child
                    };
                }

                return (manager, lp);    
            }

            throw new NotSupportedException();
        }


        protected override Size2 MeasureWork(Size2 availSize)
        {
            var (manager, lyParams) = GetLayoutManager();

            return manager.Measure(availSize, lyParams);
        }


        protected override Size2 ArrangeWork(Rect2 finalRect)
        {
            var (manager, lyParams) = GetLayoutManager();

            return manager.Arrange(finalRect, lyParams);
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
