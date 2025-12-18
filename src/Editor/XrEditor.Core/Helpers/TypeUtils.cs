using System.ComponentModel;
using System.Reflection;

namespace XrEditor
{
    public struct TypeInfo
    {
        public string Name;

        public Type Type;

        public Func<object?> CreateInstance;
    }

    public static class TypeUtils
    {

        public static IEnumerable<TypeInfo> GetTypes(Type baseType)
        {
            AssemblyName curAss = baseType.Assembly.GetName();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                AssemblyName[] refAss = assembly.GetReferencedAssemblies();

                if (baseType.Assembly != assembly && (refAss == null ||
                    !refAss.Any(a => a.FullName == curAss.FullName)))
                    continue;

                foreach (Type type in assembly.GetTypes())
                {
                    if (!baseType.IsAssignableFrom(type))
                        continue;

                    if (type.IsAbstract)
                        continue;

                    ConstructorInfo? constr = type.GetConstructor([]);
                    if (constr == null)
                        continue;

                    DisplayNameAttribute? dispName = type.GetCustomAttribute<DisplayNameAttribute>();

                    yield return new TypeInfo
                    {
                        Name = dispName?.DisplayName ?? type.Name,
                        Type = baseType,
                        CreateInstance = () => Activator.CreateInstance(type)
                    };
                }
            }
        }

        public static IEnumerable<TypeInfo> GetTypes<T>()
        {
            return GetTypes(typeof(T));
        }
    }
}
