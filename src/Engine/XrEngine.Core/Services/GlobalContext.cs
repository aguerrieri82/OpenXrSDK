namespace XrEngine
{
    public class GlobalContext
    {
        class ServiceInfo
        {
            public object? Instance;

            public Type? Type;

            public Func<object>? Factory;
        }


        readonly List<ServiceInfo> _services = [];
        readonly Dictionary<Type, object> _cache = [];

        public object Require(Type type)
        {
            if (_cache.TryGetValue(type, out var value))
                return value;

            var info = _services.FirstOrDefault(a => type.IsAssignableFrom(a.Type));

            if (info == null)
                throw new NotSupportedException();

            if (info.Instance == null)
                info.Instance = info.Factory!();

            _cache[type] = info.Instance;

            return info.Instance;
        }

        public object RequireInstance(Type type)
        {
            var info = _services.FirstOrDefault(a => type.IsAssignableFrom(a.Type));
            if (info?.Factory == null)
                throw new NotSupportedException();
            return info.Factory!();
        }

        public void Implement(Type type, object instance)
        {
            var info = _services.FirstOrDefault(a => a.Type == type);

            if (info != null)
                info.Instance = instance;
            else
            {
                _services.Add(new ServiceInfo
                {
                    Type = type,
                    Instance = instance
                });
            }
        }

        public void Implement(Type type, Func<object> factory)
        {
            _services.Add(new ServiceInfo
            {
                Type = type,
                Factory = factory
            });
        }
    }
}
