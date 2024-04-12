namespace XrEngine.Services
{
    public class MemoryStateContainer : IStateContainer
    {
        public class StateContext : IStateContext
        {
            public RefTable RefTable { get; } = new();

            public StateContextFlags Flags { get; set; }
        }

        private readonly Dictionary<string, object?> _state;

        private readonly StateContext _context;


        public MemoryStateContainer()
        {
            _context = new StateContext();
            _context.RefTable.Container = new MemoryStateContainer(_context, []);
            _state = [];
        }

        MemoryStateContainer(StateContext ctx, Dictionary<string, object?> state)
        {
            _context = ctx;
            _state = state;
        }

        public IStateContainer Enter(string key, bool resolveRef = false)
        {
            if (!_state.TryGetValue(key, out var value))
            {
                value = new MemoryStateContainer(_context, []);
                _state[key] = value;
            }

            if (resolveRef && IsRef(key))
            {
                var id = (Guid)_state[key]!;
                return _context.RefTable.Container!.Enter(id.ToString());
            }

            return (IStateContainer)value!;
        }


        public object? Read(string key, object? curObj, Type type)
        {
            var value = _state[key];

            var manager = TypeStateManager.Instance.Get(type);
            if (manager != null)
                return manager.Read(key, curObj, type, this);

            return value;
        }

        public void Write(string key, object? value)
        {
            if (value != null)
            {
                var manager = TypeStateManager.Instance.Get(value.GetType());
                if (manager != null)
                {
                    manager.Write(key, value, this);
                    return;
                }
            }

            _state[key] = value;
        }


        public bool Contains(string key)
        {
            return _state.ContainsKey(key);
        }

        public bool IsRef(string key)
        {
            return _state[key] is Guid;
        }

        public int Count => _state.Count;

        public IStateContext Context => _context;

        public IEnumerable<string> Keys => _state.Keys;

    }
}
