using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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
            var curAss = baseType.Assembly.GetName();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var refAss = assembly.GetReferencedAssemblies();

                if (baseType.Assembly != assembly && (refAss == null ||
                    !refAss.Any(a=> a.FullName == curAss.FullName)))
                    continue;

                foreach (var type in assembly.GetTypes())
                {
                    if (!baseType.IsAssignableFrom(type))
                        continue;
                    
                    if (type.IsAbstract)
                        continue;

                    var constr = type.GetConstructor([]);
                    if (constr == null)
                        continue;

                    var dispName = type.GetCustomAttribute<DisplayNameAttribute>();

                    yield return new TypeInfo
                    {
                        Name = dispName?.DisplayName ?? type.Name,  
                        Type = baseType,
                        CreateInstance = ()=> Activator.CreateInstance(type)
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
