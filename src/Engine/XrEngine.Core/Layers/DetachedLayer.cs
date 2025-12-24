namespace XrEngine
{
    public enum DetachedLayerUsage
    {
        None = 0,
        Selection = 0x1,
        Outline = 0x2,
        Gizmos = 0x4
    }

    public class DetachedLayer : BaseLayer<Object3D>, IRenderUpdate
    {
        private int _updateCount;
        private bool _isChanged;

        public DetachedLayer()
        {
            UpdateContent = true;
        }

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
            item._scene = _manager?.Scene;
            _content.Add(item);
            NotifyChanged();
            OnChanged(item, Layer3DChangeType.Added);
        }

        public void Remove(Object3D item)
        {
            if (item._scene == _manager!.Scene)
                item._scene = null;

            _content.Remove(item);
            NotifyChanged();
            OnChanged(item, Layer3DChangeType.Removed);
        }

        public void Clear()
        {
            if (_content.Count == 0)
                return;
            foreach (var item in _content)
                OnChanged(item, Layer3DChangeType.Removed);
            _content.Clear();
            NotifyChanged();
        }

        public void Update(RenderContext ctx)
        {
            if (UpdateContent)
                _content.Update(ctx);
        }

        public void Reset(bool onlySelf = false)
        {
            if (UpdateContent)
                _content.Reset(onlySelf);
        }

        int IRenderUpdate.UpdatePriority => 0;

        public DetachedLayerUsage Usage { get; set; }

        public bool UpdateContent { get; set; }
    }
}
