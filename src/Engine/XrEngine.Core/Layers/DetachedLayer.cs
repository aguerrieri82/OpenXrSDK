using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine.Layers
{
    public class DetachedLayer : BaseLayer<Object3D>, IRenderUpdate
    {
        private int _updateCount;
        private bool _isChanged;

        public void BeginUpdate()
        {
            _updateCount++;
        }

        public void EndUpdate()
        {
            _updateCount--;

            if (_updateCount == 0 && _isChanged)
            {
                NotifyChanged();
                _isChanged = false;
            }
        }

        protected void NotifyChanged()
        {
            if (_updateCount > 0)
            {
                _isChanged = true;
                return;
            }
            _version++;
        }


        public void Add(Object3D item)
        {
            item._scene = _manager!.Scene;
            _content.Add(item);
            NotifyChanged();
        }
        
        public void Remove(Object3D item)
        {
            if (item._scene == _manager!.Scene)
                item._scene = null;

            _content.Remove(item);
            NotifyChanged();
        }

        public void Clear()
        {
            if (_content.Count == 0)
                return;
            _content.Clear();
            NotifyChanged();
        }

        public void Update(RenderContext ctx)
        {
            _content.Update(ctx);
        }

        public void Reset(bool onlySelf = false)
        {
            _content.Reset(onlySelf);
        }
    }
}
