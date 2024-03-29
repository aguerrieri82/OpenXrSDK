using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace XrEngine.Services
{
    public class JsonStateContainer : IStateContainer
    {
        JsonObject _state;
        JsonArray _refs;
        Dictionary<object, int> _resolvedRefs;

        public JsonStateContainer()
        {
            var main = new JsonObject();
            _refs = new JsonArray();
            _state = new JsonObject();
            _resolvedRefs = [];
            main["refs"] = _refs;
            main["root"] = _state;
        }

        public JsonStateContainer(string json)
        {
            var main = JsonSerializer.Deserialize<JsonObject>(json)!;
            _state = (JsonObject)main["root"]!;
            _refs = (JsonArray)main["refs"]!;
            _resolvedRefs = [];
        }

        internal JsonStateContainer(JsonObject state, JsonArray refs, Dictionary<object, int> resolvedRefs)
        {
            _refs = refs;
            _state = state;
            _resolvedRefs = resolvedRefs;
        }

        public IStateContainer Enter(string key)
        {
            if (!_state.ContainsKey(key))
            {
                var newObj = new JsonObject();
                _state[key] = newObj;
            }
            return new JsonStateContainer((JsonObject)_state[key]!, _refs, _resolvedRefs);
        }

        public T Read<T>(string key)
        {
            var value = _state[key]!;    
            if (value is JsonValue val && val.GetValueKind() == JsonValueKind.String)
            {
                var strValue = value.GetValue<string>();

                if (strValue.StartsWith("$(ref):"))
                {
                    var index = int.Parse(strValue.Substring(7));
                    var curObj = _resolvedRefs.Where(a => a.Value == index).Select(a=> a.Key).FirstOrDefault();
                    if (curObj == null)
                    {
                        curObj = _refs[index].Deserialize<T>()!;
                        _resolvedRefs[curObj] = index;
                    }
                    return (T)curObj;
                }
            }
            return value.Deserialize<T>()!;
        }

        public void Write(string key, object value)
        {
            _state[key] = JsonSerializer.SerializeToNode(value);
        }

        public void WriteRef(string key, object value)
        {
            if (!_resolvedRefs.TryGetValue(value, out var index))
            {
                index = _refs.Count;
                _resolvedRefs[value] = index;
                _refs.Add(JsonSerializer.SerializeToNode(value));
            }
            _state[key] = "$(ref):" + index;
        }

        public int Count => _state.Count;

        public IEnumerable<string> Keys => _state.Select(a=> a.Key);

    }
}
