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
        protected readonly JsonObject? _main;
        protected readonly JsonObject _state;
        protected readonly JsonStateContainer _refs;
        protected readonly Dictionary<object, int> _resolvedRefs;
        protected readonly StateContext _context;

        public JsonStateContainer(StateContext ctx)
        {
            _main = new JsonObject();
            _refs = new JsonStateContainer(ctx, new JsonObject());
            _state = new JsonObject();
            _resolvedRefs = [];
            _context = ctx;
            _main["refs"] = _refs._state;
            _main["root"] = _state;
        }

        public JsonStateContainer(string json, StateContext ctx)
        {
            _main = JsonSerializer.Deserialize<JsonObject>(json)!;
            _state = (JsonObject)_main["root"]!;
            _refs = new JsonStateContainer(ctx, (JsonObject)_main["refs"]!);
            _resolvedRefs = [];
            _context = ctx;
        }

        protected JsonStateContainer(StateContext ctx, JsonObject state)
        {
            _state = state;
            _refs = this;
            _resolvedRefs = [];
            _context = ctx;
        }

        protected JsonStateContainer(JsonObject state, JsonStateContainer refs, Dictionary<object, int> resolvedRefs, StateContext ctx)
        {
            _refs = refs;
            _state = state;
            _context = ctx; 
            _resolvedRefs = resolvedRefs;
        }

        public IStateContainer Enter(string key)
        {
            if (!_state.ContainsKey(key))
            {
                var newObj = new JsonObject();
                _state[key] = newObj;
            }
            return new JsonStateContainer((JsonObject)_state[key]!, _refs, _resolvedRefs, _context);
        }

        public object? Read(string key, Type type)
        {
            if (type.IsClass && type != typeof(string) && _state[key] is JsonObject)
            {
                var objState = Enter(key);
                if (objState.Contains("$ref"))
                {
                    var index = objState.Read<int>("$ref");
                    var curObj = _resolvedRefs.Where(a => a.Value == index).Select(a => a.Key).FirstOrDefault();
                    if (curObj == null)
                    {
                        curObj = _refs.Read(index.ToString(), type);
                        _resolvedRefs[curObj!] = index;
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
            if (value != null)
            {
                var manager = TypeStateManager.Instance.Get(value.GetType());
                if (manager != null)
                {
                    manager.Write(key, value, this, _context);
                    return;
                }
            }

            _state[key] = JsonSerializer.SerializeToNode(value);
        }

        public void WriteRef(string key, object? value)
        {
            if (value == null)
                Write(key, null);
            else
            {
                if (!_resolvedRefs.TryGetValue(value, out var index))
                {
                    index = _refs.Count;
                    _resolvedRefs[value] = index;
                    _refs.Write(index.ToString(), value);
                }

                Enter(key).Write("$ref", index);
            }
        }

        public bool Contains(string key)
        {
            return _state.ContainsKey(key);
        }

        public string AsJson()
        {
            return (_state).ToJsonString(new JsonSerializerOptions { WriteIndented = true });   
        }

        public void Clear()
        {
            _resolvedRefs.Clear();
        }

        public int Count => _state.Count;

        public IEnumerable<string> Keys => _state.Select(a=> a.Key);
    }
}
