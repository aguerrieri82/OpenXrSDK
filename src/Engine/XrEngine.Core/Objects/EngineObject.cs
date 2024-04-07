namespace XrEngine
{
    [Flags]
    public enum EngineObjectFlags
    {
        None = 0,
        Generated = 0x1,
        ChildGenerated = 0x2
    }

    public abstract class EngineObject : IComponentHost, IRenderUpdate, IDisposable, IStateObject
    {
        protected Dictionary<string, object?>? _props;
        protected List<IComponent>? _components;
        protected ObjectId _id;
        protected ObjectChangeSet _lastChanges;
        protected int _updateCount;


        public void SetState(IStateContainer container)
        {
            BeginUpdate();
            SetStateWork(container);
            EndUpdate();
        }

        public virtual void GetState(IStateContainer container)
        {
            container.Write(nameof(Id), _id.Value);

            if (_components != null)
                container.WriteArray(nameof(Components), _components);
        }

        protected virtual void SetStateWork(IStateContainer container)
        {
            _id.Value = container.Read<uint>(nameof(Id));
            _components ??= [];
            container.ReadArray(nameof(Components), _components, a => AddComponent(a), RemoveComponent);
        }

        public void BeginUpdate()
        {
            _updateCount++;
        }

        public void EndUpdate()
        {
            _updateCount--;

            if (_updateCount == 0 && _lastChanges.Changes != null)
            {
                foreach (var change in _lastChanges.Changes)
                    NotifyChanged(change);
                _lastChanges.Clear();
            }
        }

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

            component.EnsureId();
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

        public void NotifyChanged(ObjectChange change)
        {
            if (_updateCount > 0)
            {
                _lastChanges.Add(change);
                return;
            }

            OnChanged(change);
        }

        protected virtual void OnChanged(ObjectChange change)
        {
            Changed?.Invoke(this, change);
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


        public virtual void Dispose()
        {
            if (_components != null)
            {
                foreach (var component in _components.OfType<IDisposable>())
                    component.Dispose();
                _components = null;
            }

            if (_props != null)
            {
                foreach (var component in _props.Values.OfType<IDisposable>())
                    component.Dispose();
                _props = null;
            }

            GC.SuppressFinalize(this);
        }

        public void EnsureId()
        {
            if (_id.Value == 0)
                _id = ObjectId.New();
        }

        public virtual void Reset(bool onlySelf = false)
        {
            if (_components != null)
            {
                foreach (var item in _components.OfType<IRenderUpdate>())
                    item.Reset();
            }
        }

        public event Action<EngineObject, ObjectChange>? Changed;

        public long Version { get; set; }

        public EngineObjectFlags Flags { get; set; }

        public ObjectId Id => _id;
    }
}
