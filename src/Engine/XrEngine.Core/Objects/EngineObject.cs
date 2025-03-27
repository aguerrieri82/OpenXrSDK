namespace XrEngine
{
    [Flags]
    public enum EngineObjectFlags
    {
        None = 0,
        Generated = 0x1,
        ChildrenGenerated = 0x2,
        Readonly = 0x4,
        EnableDebug = 0x8,
        DisableNotifyChangedScene = 0x10,
        NotifyChangedScene = 0x20,
        GpuOnly = 0x40,
        Mutable = 0x60,
        NotifyChanged = 0x80,
        NoFrustumCulling = 0x100,
        LargeOccluder = 0x200
    }

    public abstract class EngineObject : IComponentHost, IRenderUpdate, IDisposable, IStateObject
    {
        protected Dictionary<string, object?>? _props;
        protected List<IComponent>? _components;
        protected ObjectId _id;
        protected ObjectChangeSet _lastChanges;
        protected int _updateCount;

        public EngineObject()
        {
            Flags = EngineObjectFlags.EnableDebug | EngineObjectFlags.NotifyChanged;
        }

        public void SetState(IStateContainer container)
        {
            BeginUpdate();
            SetStateWork(container);
            EndUpdate();
        }

        public virtual void GetState(IStateContainer container)
        {
            EnsureId();

            container.Write(nameof(Id), _id.Value);

            if (_components != null)
                container.WriteArray(nameof(Components), _components);
        }

        protected virtual void SetStateWork(IStateContainer container)
        {
            _id.Value = container.Read<Guid>(nameof(Id));
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
            if (_components == null || ctx.UpdateOnlySelf)
                return;

            _components.OfType<IRenderUpdate>().Update(ctx, false);
        }

        public IReadOnlyList<IComponent> Components()
        {
            if (_components == null)
                return [];
            return _components;
        }

        public virtual T AddComponent<T>(T component) where T : IComponent
        {
            if (component.Host == this)
                return component;

            component.Host?.RemoveComponent(component);

            component.EnsureId();
            component.Attach(this);

            _components ??= [];
            _components.Add(component);

            NotifyChanged(new ObjectChange(ObjectChangeType.ComponentAdd, component));

            return component;
        }

        public virtual void RemoveComponent(IComponent component)
        {
            if (component.Host != this)
                return;

            component.Detach();

            _components?.Remove(component);

            NotifyChanged(new ObjectChange(ObjectChangeType.ComponentRemove, component));
        }

        public void NotifyChanged(ObjectChange change)
        {
            if ((Flags & EngineObjectFlags.NotifyChanged) == 0)
                return;

            if (_updateCount > 0)
            {
                _lastChanges.Add(change);
                return;
            }

            change.Target ??= this;

            OnChanged(change);
        }

        protected virtual void OnChanged(ObjectChange change)
        {
            Version++;
            Changed?.Invoke(this, change);
        }

        public void DeleteProp(string name)
        {
            _props?.Remove(name);
        }

        public void SetProp(string name, object? value)
        {
            _props ??= [];
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
                foreach (var prop in _props.Values.OfType<IDisposable>())
                    prop.Dispose();
                foreach (var prop in _props.Values.OfType<IObjectTool>())
                    prop.Deactivate();
                _props = null;
            }

            ObjectBinder.Unbind(this);

            GC.SuppressFinalize(this);
        }

        public void EnsureId()
        {
            if (_id.Value == Guid.Empty)
                _id = Utils.HashGuid(GeneratePath());

        }

        public string GeneratePath()
        {
            var parts = new List<string>();
            GeneratePath(parts);
            return string.Join("/", parts);
        }

        public virtual void GeneratePath(List<string> parts)
        {

        }

        public virtual void Reset(bool onlySelf = false)
        {
            if (_components != null)
            {
                foreach (var item in _components.OfType<IRenderUpdate>())
                    item.Reset();
            }
        }

        public long Version { get; protected set; }

        public EngineObjectFlags Flags { get; set; }

        public ObjectId Id
        {
            get => _id;
            set => _id = value;
        }

        public event Action<EngineObject, ObjectChange>? Changed;

        int IRenderUpdate.UpdatePriority => 0;
    }
}
