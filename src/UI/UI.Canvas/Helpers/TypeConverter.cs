using System.Reflection;

namespace CanvasUI
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

            IEnumerable<MethodBase> methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public)
                              .Where(a => a.Name == "op_Implicit")
                              .Cast<MethodBase>()
                              .Union(type.GetConstructors());

            foreach (MethodBase? op in methods)
            {
                if ((op is MethodInfo mi && mi.ReturnType == type) || op.IsConstructor)
                {
                    ParameterInfo[] args = op.GetParameters();

                    if (args.Length != 1)
                        continue;

                    Type argType = op.GetParameters()[0].ParameterType;

                    object? argValue = value;

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
