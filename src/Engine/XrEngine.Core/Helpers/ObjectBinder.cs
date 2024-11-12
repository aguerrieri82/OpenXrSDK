using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine
{
    public static class ObjectBinder
    {
        static Dictionary<object, object> _bindings = [];

        public static void Bind(object a, object b)
        {
            _bindings[a] = b;
            _bindings[b] = a;
        }

        public static void Unbind(object a)
        {
            if (_bindings.TryGetValue(a, out var b))
            {
                _bindings.Remove(a);
                _bindings.Remove(b);
            }
        }

        public static void Dispose(object a)
        {
            if (_bindings.TryGetValue(a, out var b))
            {
                _bindings.Remove(a);
                _bindings.Remove(b);

                if (b is IDisposable disposable)
                    disposable.Dispose();   
            }   
        }   

        public static T Get<T>(object a) where T : class
        {
            return (T)_bindings[a];
        }


        public static bool TryGet<T>(object a, [NotNullWhen(true)]out T? result) where T : class
        {
            result = null;
            return _bindings.TryGetValue(a, out var obj) && (result = obj as T) != null;
        }
    }
}
