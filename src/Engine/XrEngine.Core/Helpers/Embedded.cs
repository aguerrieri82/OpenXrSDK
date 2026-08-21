using System.Collections.Concurrent;
using System.Reflection;

namespace XrEngine
{
    public static class Embedded
    {
        static readonly ConcurrentDictionary<string, string?> _resCache = [];
        static readonly HashSet<Assembly> _assemblies = [];

        public static string GetString(string resName)
        {
            return GetString<Module>(resName);
        }

        public static string GetString<T>(string resName)
        {
            return GetString(typeof(T).Assembly, resName);
        }

        public static string GetString(Assembly ctx, string resName)
        {
            var result = TryGetString(ctx, resName);

            if (result == null)
            {
                Log.Warn("RESOURCES", "Req: '{0}'\n{1}", resName, string.Join("\n", ctx.GetManifestResourceNames()));
                throw new InvalidOperationException($"Resource '{resName}' not found in assembly '{ctx.FullName}'");
            }

            return result;
        }

        public static string? TryGetString(string resName)
        {
            return TryGetString<Module>(resName);
        }

        public static string? TryGetString<T>(string resName)
        {
            return TryGetString(typeof(T).Assembly, resName);
        }

        public static string? TryGetString(Assembly ctx, string resName)
        {
            if (resName.StartsWith('['))
            {
                var index = resName.IndexOf(']');
                var assName = resName[1..index];

                lock (_assemblies)
                    ctx = _assemblies.First(a => a.GetName().Name == assName);

                resName = resName[(index + 1)..];
            }

            var reqName = $"{ctx.GetName().Name}:{resName}";

            return _resCache.GetOrAdd(reqName, key =>
            {
                resName = resName.Replace('/', '.');

                if (!resName.StartsWith('/'))
                    resName = "." + resName;

                var fullName = ctx.GetManifestResourceNames()
                    .SingleOrDefault(a => a.Contains(resName, StringComparison.CurrentCultureIgnoreCase));

                if (fullName == null)
                    return null;

                using var stream = ctx.GetManifestResourceStream(fullName);
                using var reader = new StreamReader(stream!);

                return reader.ReadToEnd();
            });
        }

        public static void Register(Assembly assembly)
        {
            _assemblies.Add(assembly);
        }
    }
}
