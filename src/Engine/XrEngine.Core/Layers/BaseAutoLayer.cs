using System.Diagnostics;

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

        protected override void NotifyChangedWork(Object3D sender, ObjectChange change)
        {
            if (sender is T tObj && AffectChange(change))
            {
                EngineApp.Current!.Stats.LayerChanges++;
                if (change.IsAny(ObjectChangeType.SceneRemove) || !BelongsToLayer(tObj))
                    Remove(tObj);
                else
                    Add(tObj);
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
            _version++;
            OnAdded(obj);
        }

        protected void Remove(T obj)
        {
            if (!Contains(obj))
                return;
            _content.Remove(obj);
            _version++;
            OnRemoved(obj);
        }

        protected virtual void OnRemoved(T obj)
        {
            _manager?.NotifyObjectRemoved(this, obj);
            OnChanged(obj, Layer3DChangeType.Removed);
        }

        protected virtual void OnAdded(T obj)
        {
            _manager?.NotifyObjectAdded(this, obj);
            OnChanged(obj, Layer3DChangeType.Added);
        }

        protected abstract bool BelongsToLayer(T obj);
    }
}
