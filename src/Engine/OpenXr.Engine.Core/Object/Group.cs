using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
    public class Group : Object3D
    {
        protected List<Object3D> _children;

        public Group()
        {
            _children = [];
        }


        public override bool UpdateWorldMatrix(bool updateChildren, bool updateParent)
        {
            var isChanged = base.UpdateWorldMatrix(updateChildren, updateParent);

            if (updateChildren && isChanged)
                _children.ForEach(a => a.UpdateWorldMatrix(true, false));

            return isChanged;   
        }


        public override void Update(RenderContext ctx)
        {
            base.Update(ctx);

            UpdateSelf(ctx);

            _children.Update(ctx);
        }

        protected virtual void UpdateSelf(RenderContext ctx)
        {

        }

        public IEnumerable<Object3D> Descendants()
        {
            return Descendants<Object3D>();
        }

        public IEnumerable<T> Descendants<T>() where T : Object3D   
        {
            foreach (var child in _children)
            {
                if (child is T validChild)
                    yield return validChild;

                if (child is Group group)
                {
                    foreach (var desc in group.Descendants<T>())
                        yield return desc;  
                }
            }
        }

        public T AddChild<T>(T child) where T : Object3D
        {
            if (child.Parent == this)
                return child;

            if (child.Parent != null)
                child.Parent.RemoveChild(child);

            child.Parent = this;
            _children.Add(child);
            return child;
        }

        public void RemoveChild(Object3D child)
        {
            if (child.Parent != this)
                return;

            _children.Remove(child);
            child.Parent = null;
        }

        public IReadOnlyList<Object3D> Children => _children.AsReadOnly();
    }
}
