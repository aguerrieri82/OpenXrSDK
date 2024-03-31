using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using XrMath;

namespace XrEngine
{
    public static class TypeExtensions
    {
        public static bool HasEmptyConstructor(this Type type)
        {
            foreach (var c in type.GetConstructors())
            {
                if (c.IsPublic && c.GetParameters().Length == 0)    
                    return true;    
            }

            return false;
        }
    }
}
