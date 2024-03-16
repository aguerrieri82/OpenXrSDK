using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine.UI
{
    public static class TypeConverter
    {
        public static bool TryConvert(object? value, Type type, out object? result)
        {
            if (value == null)
            {
                result = null;
                return true;
            }

            if (type.IsAssignableFrom(value.GetType()))
            {
                result = value;
                return true;
            }

            var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public)
                              .Where(a => a.Name == "op_Implicit")
                              .Cast<MethodBase>()
                              .Union(type.GetConstructors());

            foreach (var op in methods)
            {
                if ((op is MethodInfo mi && mi.ReturnType == type) || op.IsConstructor)
                {
                    var args = op.GetParameters();

                    if (args.Length != 1)
                        continue;

                    var argType = op.GetParameters()[0].ParameterType;

                    var argValue = value;

                    if (!argType.IsAssignableFrom(value.GetType()))
                    {
                        if (!TryConvert(value, argType, out argValue))
                            continue;
                    }

                    result = op.Invoke(null, [argValue]);
                    return true;
                }
            }

            result = null;
            return false;   
        }
    }
}
