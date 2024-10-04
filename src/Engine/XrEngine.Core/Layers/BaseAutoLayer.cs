using System.Diagnostics;
using XrEngine.Layers;

namespace XrEngine
{
    public abstract class BaseAutoLayer<T> : BaseLayer<T> where T : ILayer3DItem
    {

        protected void Rebuild()
        {
            while (_content.Count > 0)
                Remove(_content.First());

            foreach (var obj in _manager!.Scene!.Descendants().OfType<T>())
            {
                if (obj is T tObj && BelongsToLayer(tObj))
                    Add(tObj);
            }
        }

        protected override void OnEnabledChanged()
        {
            if (IsEnabled)
                Rebuild();
            base.OnEnabledChanged();
        }


        protected virtual bool AffectChange(ObjectChange change)
        {
            return true;
        }

        public override void NotifyChanged(Object3D obj, ObjectChange change)
        {
            if (!IsEnabled)
                return;

            
            if (obj is T tObj && AffectChange(change))
            {
                if (change.IsAny(ObjectChangeType.SceneRemove) || !BelongsToLayer(tObj))
                    Remove(tObj);
                else
                    Add(tObj);
            }

            base.NotifyChanged(obj, change);
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
