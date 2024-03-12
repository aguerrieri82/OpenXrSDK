﻿namespace XrEngine
{
    public abstract class EngineObject : IComponentHost, IRenderUpdate, IDisposable
    {
        protected Dictionary<string, object?>? _props;
        protected List<IComponent>? _components;
        protected ObjectId _id;

        public virtual void Update(RenderContext ctx)
        {
            if (_components == null)
                return;

            _components.OfType<IRenderUpdate>().Update(ctx);
        }

        public IEnumerable<T> Components<T>() where T : IComponent
        {
            if (_components == null)
                return [];
            return _components.OfType<T>();
        }

        public T AddComponent<T>(T component) where T : IComponent
        {
            if (component.Host == this)
                return component;

            if (component.Host != null)
                component.Host.RemoveComponent(component);

            component.Attach(this);

            if (_components == null)
                _components = [];

            _components.Add(component);

            NotifyChanged(ObjectChangeType.Components);

            return component;
        }

        public void RemoveComponent(IComponent component)
        {
            if (component.Host != this)
                return;

            component.Detach();

            _components!.Remove(component);

            NotifyChanged(ObjectChangeType.Components);
        }

        public virtual void NotifyChanged(ObjectChange change)
        {

        }

        public void SetProp(string name, object? value)
        {
            if (_props == null)
                _props = [];
            _props[name] = value;
        }

        public T? GetProp<T>(string name)
        {
            var result = GetProp(name);
            if (result == null)
                return default;
            return (T)result;
        }

        public object? GetProp(string name)
        {
            if (_props == null)
                return null;
            if (_props.TryGetValue(name, out var value))
                return value;
            return null;
        }

        internal void EnsureId()
        {
            if (_id == 0)
                _id = ObjectId.New();
        }

        public virtual void Dispose()
        {
            if (_components != null)
            {
                foreach (var component in _components.OfType<IDisposable>())
                    component.Dispose();
                _components = null;
            }
            GC.SuppressFinalize(this);
        }

        public long Version { get; set; }

        public ObjectId Id => _id;
    }
}