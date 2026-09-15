using System.Runtime.InteropServices;

namespace Common.Interop
{
    public static class MarshalCache
    {
        [ThreadStatic]
        static Dictionary<Type, int>? _cache;

        public static int SizeOf(Type type)
        {
            _cache ??= [];

            if (!_cache.TryGetValue(type, out var size))
            {
                var marshalType = type.IsEnum ? Enum.GetUnderlyingType(type) : type;
                size = Marshal.SizeOf(marshalType);
                _cache[type] = size;
            }

            return size;
        }
    }
}