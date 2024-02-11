using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
    public abstract class BaseAutoLayer<T> : ILayer where T : ILayerObject
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

        public virtual void NotifyChanged(Object3D obj, ObjectChange change)
        {
            if (obj is T tObj)
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


        public IEnumerable<ILayerObject> Content => (IEnumerable<ILayerObject>)_content;

        public bool IsVisible { get; set; }

        public ObjectId Id => _id;
    }
}
