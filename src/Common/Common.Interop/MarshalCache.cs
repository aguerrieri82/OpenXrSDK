using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

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
                size = Marshal.SizeOf(type);
                _cache[type] = size;
            }
            return size;
        }
    }
}
