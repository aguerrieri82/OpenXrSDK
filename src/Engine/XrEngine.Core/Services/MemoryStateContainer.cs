using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine.Services
{
    public class MemoryStateContainer : IStateContainer
    {
        struct ObjectRef(object obj)
        {
            public object Reference = obj;
        }


        private readonly Dictionary<string, object?> _state = [];
        private readonly StateContext _ctx;

        public MemoryStateContainer(StateContext ctx)
        {
            _ctx = ctx; 
        }

        public IStateContainer Enter(string key)
        {
            if (!_state.TryGetValue(key, out var value))
            {
                value = new MemoryStateContainer(_ctx);
                _state[key] = value;    
            }

            return (IStateContainer)value!;
        }


        public object? Read(string key, Type type)
        {
            var value = _state[key];

            if (value is ObjectRef objectRef)
                return objectRef.Reference;

            var manager = TypeStateManager.Instance.Get(type);
            if (manager != null)
                return manager.Read(key, type, this, _ctx);

            return value;
        }

        public T Read<T>(string key)
        {
            return (T)Read(key, typeof(T))!;
        }

        public void Write(string key, object? value)
        {
            if (value != null)
            {
                var manager = TypeStateManager.Instance.Get(value.GetType());
                if (manager != null)
                {
                    manager.Write(key, value, this, _ctx);
                    return;
                }
            }

            _state[key] = value;
        }

        public void WriteRef(string key, object? value)
        {
            if (value != null)
                _state[key] = new ObjectRef(value);
            else
                _state[key] = null; 
        }

        public bool Contains(string key)
        {
            return _state.ContainsKey(key); 
        }

        public int Count => _state.Count;

        public IEnumerable<string> Keys => _state.Keys;

    }
}
