using System.Reflection;

namespace OpenXr.Engine
{
    public static class Embedded
    {
        public static string GetString(string resName)
        {
            return GetString(Assembly.GetCallingAssembly(), resName);
        }

        public static string GetString<T>(string resName)
        {
            return GetString(typeof(T).Assembly, resName);
        }

        public static string GetString(Assembly ctx, string resName)
        {
            resName = resName.Replace('/', '.');
            var fullName = ctx.GetManifestResourceNames().Single(a => a.Contains(resName, StringComparison.CurrentCultureIgnoreCase));

            using var stream = ctx.GetManifestResourceStream(fullName);
            using var reader = new StreamReader(stream!);
            return reader.ReadToEnd();
        }
    }
}
