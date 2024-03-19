using System.Diagnostics;

namespace XrEngine
{
    public abstract class BaseAutoLayer<T> : ILayer3D where T : ILayer3DItem
    {
        protected readonly ObjectId _id;
        protected readonly HashSet<T> _content = [];
        protected LayerManager? _manager;

        public BaseAutoLayer()
        {
            _id = ObjectId.New();
        }

        public void Attach(LayerManager manager)
        {
            _manager = manager;

        }

        public virtual void Detach()
        {
            _manager = null;
        }

        protected virtual bool AffectChange(ObjectChange change)
        {
            return true;
        }

        public virtual void NotifyChanged(Object3D obj, ObjectChange change)
        {
            if (obj is T tObj && AffectChange(change))
            {
                if (BelongsToLayer(tObj))
                    Add(tObj);
                else
                    Remove(tObj);
            }
        }

        protected bool Contains(T obj)
        {
            Debug.Assert(_manager != null);
            return _manager.LayerContains(this, obj);
        }

        protected void Add(T obj)
        {
            if (Contains(obj))
                return;
            _content.Add(obj);
            _manager?.NotifyObjectAdded(this, obj);
        }

        protected void Remove(T obj)
        {
            if (!Contains(obj))
                return;
            _content.Remove(obj);
            _manager?.NotifyObjectRemoved(this, obj);
        }

        protected abstract bool BelongsToLayer(T obj);


        public IEnumerable<ILayer3DItem> Content => (IEnumerable<ILayer3DItem>)_content;

        public bool IsVisible { get; set; }

        public ObjectId Id => _id;
    }
}
