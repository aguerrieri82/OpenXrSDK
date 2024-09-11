using System;
using System.Diagnostics;
using XrEngine.Layers;

namespace XrEngine
{
    public abstract class BaseAutoLayer<T> : BaseLayer<T> where T : ILayer3DItem
    {

        protected virtual bool AffectChange(ObjectChange change)
        {
            return true;
        }

        public override void NotifyChanged(Object3D obj, ObjectChange change)
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
            _version++;
        }

        protected void Remove(T obj)
        {
            if (!Contains(obj))
                return;
            _content.Remove(obj);
            _manager?.NotifyObjectRemoved(this, obj);
            _version++;
        }

        protected abstract bool BelongsToLayer(T obj);



    }
}
