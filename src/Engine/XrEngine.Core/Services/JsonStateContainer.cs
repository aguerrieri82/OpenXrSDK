using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;


namespace XrEngine.Services
{
    public class JsonStateContainer : IStateContainer
    {
        const string KEY_REF = "$ref";

        protected struct References
        {
            public Dictionary<object, int> Resolved;
            public JsonStateContainer Container;
            public Dictionary<object, JsonObject> Objects;

        }
        protected readonly JsonObject? _main;
        protected readonly JsonObject _state;
        protected readonly References _refs;
        protected readonly StateContext _context;

        public JsonStateContainer(StateContext ctx)
        {
            _main = new JsonObject();
            _refs.Resolved = [];
            _refs.Objects = [];
            _refs.Container = new JsonStateContainer(ctx, new JsonObject(), _refs.Resolved, _refs.Objects);

            _state = new JsonObject();
            _context = ctx;
            _main["refs"] = _refs.Container._state;
            _main["root"] = _state;
        }

        public JsonStateContainer(string json, StateContext ctx)
        {
            _main = JsonSerializer.Deserialize<JsonObject>(json)!;
            _state = (JsonObject)_main["root"]!;
            _context = ctx;
            _refs.Resolved = [];
            _refs.Objects = [];
            _refs.Container = new JsonStateContainer(ctx, (JsonObject)_main["refs"]!, _refs.Resolved, _refs.Objects);
 
        }

        protected JsonStateContainer(StateContext ctx, JsonObject state, Dictionary<object, int> resolved, Dictionary<object, JsonObject> objects)
        {
            _state = state;
            _refs.Resolved = resolved;
            _refs.Objects = objects;
            _refs.Container = this;
            _context = ctx;
        }

        protected JsonStateContainer(JsonObject state, References refs, StateContext ctx)
        {
            _refs = refs;
            _state = state;
            _context = ctx; 
        }

        public IStateContainer Enter(string key, bool resolveRef = false)
        {
            if (!_state.ContainsKey(key))
            {
                var newObj = new JsonObject();
                _state[key] = newObj;
            }
            
            var result = (JsonObject)_state[key]!;
            
            if (resolveRef && result.ContainsKey(KEY_REF))
            {
                var index = result[KEY_REF].Deserialize<int>();
                result = (JsonObject)_refs.Container._state[index.ToString()]!;
            }

            return new JsonStateContainer(result, _refs, _context);
        }

        public object? Read(string key, Type type)
        {
            if (_state[key] == null)
                return null;

            if (type.IsClass && type != typeof(string) && _state[key] is JsonObject)
            {
                var objState = Enter(key);
                if (objState.Contains(KEY_REF))
                {
                    var index = objState.Read<int>(KEY_REF);
                    var curObj = _refs.Resolved.Where(a => a.Value == index).Select(a => a.Key).FirstOrDefault();
                    if (curObj == null)
                    {
                        curObj = _refs.Container.Read(index.ToString(), type);
                        _refs.Resolved[curObj!] = index;
                    }
                    return curObj;
                }
            }

            var manager = TypeStateManager.Instance.Get(type);
            if (manager != null)
                return manager.Read(key, type, this, _context);

            return _state[key].Deserialize(type)!;
        }


        public T Read<T>(string key)
        {
            return (T)Read(key, typeof(T))!;
        }

        public void Write(string key, object? value)
        {
            bool mustSerialize = true;

            if (value != null)
            {
                var manager = TypeStateManager.Instance.Get(value.GetType());
                if (manager != null)
                {
                    manager.Write(key, value, this, _context);
                    mustSerialize = false;   
                }
            }

            if (mustSerialize)
                _state[key] = JsonSerializer.SerializeToNode(value);

            if ( value != null && value.GetType().IsClass && _state[key] is JsonObject jObj)
                _refs.Objects[value] = jObj;
        }

        public void WriteRef(string key, object? value)
        {
            if (value == null)
                Write(key, null);
            else
            {
                if (!_refs.Resolved.TryGetValue(value, out var index))
                {
                    index = _refs.Container.Count;

                    var refKey = index.ToString();
                    
                    if (!_refs.Objects.TryGetValue(value, out var jObj))
                        _refs.Container.Write(refKey, value);
                    else
                        _refs.Container._state[refKey] = jObj;

                    _refs.Resolved[value] = index;
                }

                Enter(key).Write(KEY_REF, index);
            }
        }

        public bool Contains(string key)
        {
            if (_state == null)
                return false;
            return _state.ContainsKey(key);
        }

        public string AsJson()
        {
            return (_state).ToJsonString(new JsonSerializerOptions { WriteIndented = true });   
        }

        public void Clear()
        {
            _refs.Objects.Clear();
            _refs.Resolved.Clear();
        }

        public int Count => _state.Count;

        public IEnumerable<string> Keys => _state.Select(a=> a.Key);
    }
}
