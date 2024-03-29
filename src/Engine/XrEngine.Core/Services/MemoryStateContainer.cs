using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine.Services
{
    public class MemoryStateContainer : IStateContainer
    {
        Dictionary<string, object> _state = [];
        Dictionary<object, int> _refTable;

        struct ObjectRef
        {
            public int Index;
        }

        public MemoryStateContainer()
        {
            _refTable = [];
        }


        protected MemoryStateContainer(Dictionary<object, int> refTable)
        {
            _refTable = refTable;
        }

        public IStateContainer Enter(string key)
        {
            if (!_state.TryGetValue(key, out var value))
            {
                value = new MemoryStateContainer(_refTable);
                _state[key] = value;    
            }

            return (IStateContainer)value;
        }

        public T Read<T>(string key)
        {
            var value = _state[key];
            if (value is ObjectRef objectRef)
                value = _refTable[objectRef.Index];
            return (T)value;
        }

        public void Write(string key, object value)
        {
            _state[key] = value;
        }

        public void WriteRef(string key, object value)
        {
            if (!_refTable.TryGetValue(value, out var index))
            {
                index = _refTable.Count;
                _refTable[value] = index;
            }
            _state[key] = new ObjectRef() { Index = index };
        }

        public int Count => _state.Count;

        public IEnumerable<string> Keys => _state.Keys;

    }
}
