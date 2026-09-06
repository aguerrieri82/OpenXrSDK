using System.Collections.Concurrent;

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
        LargeOccluder = 0x200,
        Static = 0x400,
        NoLogs = 0x800,
        Secondary = 0x1000
    }

    [Flags]
    public enum InvalidateMode
    {
        Object = 1,
        Content = 2,
        All = Object | Content
    }

    public static class DynamicPropRegistry
    {
        private static readonly ConcurrentDictionary<string, int> _stringToId = new();
        private static int _nextId = -1;

        public static int GetId(string name)
        {
            return _stringToId.GetOrAdd(name, _ => Interlocked.Increment(ref _nextId));
        }
    }

    public struct DynamicProp
    {
        public DynamicProp(string name)
        {
            Name = name;
            Id = DynamicPropRegistry.GetId(name);
        }

        public static implicit operator DynamicProp(string name)
        {
            return new DynamicProp(name);
        }

        public static implicit operator int(DynamicProp prop)
        {
            return prop.Id;
        }

        public string Name;

        public int Id;
    }

    public enum ObjectCloneFlags
    {
        None = 0x0,
        CloneGeometry = 0x1,
        CloneMaterials = 0x2,
        CloneComponents = 0x4,
    }

    [StateManager(StateManagerMode.Manual)]
    public abstract class EngineObject : IComponentHost, IRenderUpdate, IDisposable, IStateObject, ICloneable
    {
        protected object?[]? _props;
        protected List<IComponent>? _components;
        protected ObjectId _id;
        protected ObjectChangeSet _lastChanges;
        protected int _updateCount;

        protected long _version;

        protected long _contentVersion;

        private bool _isDisposed;

        public EngineObject()
        {
            Flags = EngineObjectFlags.EnableDebug | EngineObjectFlags.NotifyChanged;
        }

        public virtual T? Feature<T>() where T : class
        {
            if (this is T tInt)
                return tInt;
            return _components?.OfType<T>().FirstOrDefault();
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
            EngineApp.VerifyMainThread(this);

            if (component.Host == this)
                return component;

            component.Host?.RemoveComponent(component);

            component.EnsureId();
            component.Attach(this);

            _components ??= [];
            _components.Add(component);

            NotifyChanged(new ObjectChange(ChangeType.ComponentAdd, component));

            Invalidate(InvalidateMode.Content);

            return component;
        }

        public virtual void RemoveComponent(IComponent component)
        {
            EngineApp.VerifyMainThread(this);

            if (component.Host != this)
                return;

            component.Detach();

            _components?.Remove(component);

            NotifyChanged(new ObjectChange(ChangeType.ComponentRemove, component));
        }

        public void NotifyChanged(ObjectChange change)
        {
            if (!this.Is(EngineObjectFlags.NotifyChanged))
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
            Invalidate();
            Changed?.Invoke(this, change);
        }

        public void DeleteProp(int propId)
        {
            if (_props != null && propId < _props.Length)
                _props[propId] = null;
        }

        public void SetProp(int propId, object? value)
        {
            _props ??= [];
            if (propId >= _props.Length)
            {
                var newSize = Math.Max(propId + 1, _props.Length * 2);
                Array.Resize(ref _props, newSize);
            }
            _props[propId] = value;
        }

        public T? GetProp<T>(int propId)
        {
            var result = GetProp(propId);
            if (result == null)
                return default;
            return (T)result;
        }

        public object? GetProp(int propId)
        {
            if (_props != null && propId < _props.Length)
                return _props[propId];
            return null;
        }

        public virtual void Dispose()
        {
            if (_components != null)
            {
                foreach (var component in _components)
                    component.Detach(true);

                _components.Clear();
                _components = null;
            }

            if (_props != null)
            {
                foreach (var prop in _props.OfType<IDisposable>())
                    prop.Dispose();
                foreach (var prop in _props.OfType<IObjectTool>())
                    prop.Deactivate();
                _props = null;
            }

            ObjectBinder.Unbind(this);

            GC.SuppressFinalize(this);

            _isDisposed = true;
        }

        public void EnsureId()
        {
            if (_id.Value == Guid.Empty)
                _id = Guid.NewGuid();

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

        public virtual void Invalidate(InvalidateMode mode = InvalidateMode.Object)
        {
            if ((mode & InvalidateMode.Object) == InvalidateMode.Object)
                _version++;

            if ((mode & InvalidateMode.Content) == InvalidateMode.Content)
                _contentVersion++;
        }

        object ICloneable.Clone()
        {
            return Clone(ObjectCloneFlags.None);
        }

        public virtual EngineObject Clone(ObjectCloneFlags flags)
        {
            var newObj = (EngineObject)Activator.CreateInstance(GetType())!;

            CloneWork(newObj, flags);

            return newObj;
        }

        protected virtual void CloneWork(EngineObject newObj, ObjectCloneFlags flags)
        {
            if (_components != null && _components.Count > 0 && (flags & ObjectCloneFlags.CloneComponents) != 0)
            {
                foreach (var comp in _components)
                {
                    if (comp is ICloneable cloneable)
                        newObj.AddComponent((IComponent)cloneable.Clone());
                    else
                        Log.Warn(this, "{0} is not clonable", comp.GetType().FullName);
                }
            }

            newObj.Flags = Flags;
            newObj.Tag = Tag;
        }

        public EngineObjectFlags Flags { get; set; }

        public object? Tag { get; set; }

        public long ContentVersion => _contentVersion;

        public long Version => _version;

        public bool IsDisposed => _isDisposed;

        public ObjectId Id
        {
            get => _id;
            set => _id = value;
        }

        public event Action<EngineObject, ObjectChange>? Changed;

        int IRenderUpdate.UpdatePriority => 0;
    }
}
