using System.Diagnostics.CodeAnalysis;

namespace XrEngine
{

    public static class Context
    {
        public static T Require<T>() where T : class
        {
            if (!TryRequire<T>(out var instance))
                throw new InvalidOperationException($"Service {typeof(T).Name} not found");
            return instance;
        }

        public static bool TryRequire<T>([NotNullWhen(true)] out T? result) where T : class
        {
            result = (T?)Current.TryRequire(typeof(T));
            return result != null;
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


        public static GlobalContext Current { get; set; } = new();
    }
}
