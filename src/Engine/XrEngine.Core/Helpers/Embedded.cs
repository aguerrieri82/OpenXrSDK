using System.Reflection;

namespace XrEngine
{
    public static class Embedded
    {
        static readonly Dictionary<string, string> _resCache = [];
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
            var reqName = $"{ctx.GetName().Name}:{resName}";

            if (!_resCache.TryGetValue(reqName, out var result))
            {
                resName = resName.Replace('/', '.');

                if (!resName.StartsWith('/'))
                    resName = "." + resName;

                var fullName = ctx.GetManifestResourceNames().SingleOrDefault(a => a.Contains(resName, StringComparison.CurrentCultureIgnoreCase));

                if (fullName == null)
                    return null;

                using var stream = ctx.GetManifestResourceStream(fullName);
                using var reader = new StreamReader(stream!);

                result = reader.ReadToEnd();

                _resCache[reqName] = result;
            }
            return result;
        }

        public static void Register(Assembly assembly)
        {
            _assemblies.Add(assembly);
        }
    }
}
