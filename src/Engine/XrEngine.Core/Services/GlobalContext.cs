using System.Collections.Concurrent;

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
        readonly ConcurrentDictionary<Type, object?> _cache = [];

        public object? TryRequire(Type type)
        {
            return _cache.GetOrAdd(type, type =>
            {
                var info = _services.FirstOrDefault(a => type.IsAssignableFrom(a.Type));

                if (info == null)
                    return null;

                info.Instance ??= info.Factory!();

                return info.Instance;
            });
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
            {
                info.Instance = instance;
                _cache.Clear();
            }
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
