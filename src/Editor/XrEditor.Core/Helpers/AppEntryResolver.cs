using System.Reflection;
using XrEngine.OpenXr;

namespace XrEngine
{
    public static class AppEntryResolver
    {
        public static XrEngineApp Build(string entry, XrEngineAppBuilder builder)
        {
            var parts = entry.Split("::");

            if (parts.Length is < 2 or > 3)
                throw new ArgumentException($"Invalid entry '{entry}'");

            var assemblyName = parts[0];
            var typeName = parts[1];
            var methodName = parts.Length == 3 ? parts[2] : null;

            var assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == assemblyName);

            if (assembly == null)
                throw new InvalidOperationException($"Assembly '{assemblyName}' not loaded");

            var type = assembly.GetType(typeName);

            if (type == null)
                throw new InvalidOperationException($"Type '{typeName}' not found in assembly '{assemblyName}'");

            if (methodName != null)
                return BuildFromMethod(type, methodName, builder);

            return BuildFromType(type, builder);
        }

        private static XrEngineApp BuildFromType(Type type, XrEngineAppBuilder builder)
        {
            if (!typeof(IAppBuilder).IsAssignableFrom(type))
                throw new InvalidOperationException($"Type '{type.FullName}' does not implement {nameof(IAppBuilder)}");

            if (Activator.CreateInstance(type) is not IAppBuilder appBuilder)
                throw new InvalidOperationException($"Cannot create instance of '{type.FullName}'");

            return appBuilder.Build(builder);
        }

        private static XrEngineApp BuildFromMethod(Type type, string methodName, XrEngineAppBuilder builder)
        {
            var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, [typeof(XrEngineAppBuilder)]);

            if (method == null || (method.ReturnType != typeof(XrEngineApp) && method.ReturnType != typeof(XrEngineAppBuilder)))
                throw new InvalidOperationException(
                    $"Static method '{type.FullName}::{methodName}' must return {nameof(XrEngineApp)} or {nameof(XrEngineAppBuilder)}");

            var result = method.Invoke(null, [builder]);

            if (result is XrEngineApp app)
                return app;

            return ((XrEngineAppBuilder)result!).Build();
        }
    }
}