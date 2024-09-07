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

        public object Require(Type type)
        {
            var info = _services.FirstOrDefault(a => type.IsAssignableFrom(a.Type));
            if (info == null)
                throw new NotSupportedException();
            if (info.Instance == null)
                info.Instance = info.Factory!();
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

    public static class Context
    {
        public static T Require<T>() where T : class
        {
            return (T)Current.Require(typeof(T));
        }

        public static T RequireInstance<T>() where T : class
        {
            return (T)Current.RequireInstance(typeof(T));
        }


        public static void Implement<T>() where T : class, new()
        {
            Current.Implement(typeof(T), () => new T());
        }

        public static void Implement<T>(T instance) where T : notnull
        {
            Current.Implement(typeof(T), instance);
        }

        public static void Implement<T>(Func<T> factory) where T : class
        {
            Current.Implement(typeof(T), factory);
        }


        public static GlobalContext Current = new();
    }
}
