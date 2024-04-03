using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;


namespace XrEngine.Services
{
    public struct JsonStateContainer : IStateContainer
    {
        const string KEY_REF = "$ref";

        class StateContext : IStateContext
        {
            public StateContext(JsonObject main)
            {
                RefTable = new RefTable();
                Main = main;
            }

            public RefTable RefTable { get; }

            public JsonObject Main { get;  }
        }

        readonly JsonObject _state;
        readonly StateContext _context;

        public JsonStateContainer()
        {
            _context = new StateContext(new JsonObject());
            _state = [];
            var refs = new JsonObject();
            _context.Main["Root"] = _state;
            _context.Main["Refs"] = refs;
            _context.RefTable.Container = new JsonStateContainer(_context, refs);
        }

        public JsonStateContainer(string json)
        {
            _context = new StateContext(JsonSerializer.Deserialize<JsonObject>(json)!);
            _state = (JsonObject)_context.Main["Root"]!;
            _context.RefTable.Container = new JsonStateContainer(_context, (JsonObject)_context.Main["Refs"]!);
        }

        JsonStateContainer(StateContext context, JsonObject obj)
        {
            _context = context;
            _state = obj;
        }

        public readonly IStateContainer Enter(string key, bool resolveRef = false)
        {
            JsonObject result;

            if (!_state.ContainsKey(key))
            {
                result = [];
                _state[key] = result;
            }

            if (resolveRef && IsRef(key))
            {
                var id = this.Read<ObjectId>(key);
                return _context.RefTable.Container!.Enter(id.ToString());
            }

            result = (JsonObject)_state[key]!;
            return new JsonStateContainer(_context, result);
        }

        public readonly object? Read(string key, Type type)
        {
            if (_state[key] == null)
                return null;

            var manager = TypeStateManager.Instance.Get(type);
            if (manager != null)
                return manager.Read(key, type, this);

            return _state[key].Deserialize(type)!;
        }

        public readonly void Write(string key, object? value)
        {
            bool mustSerialize = true;

            if (value != null)
            {
                var manager = TypeStateManager.Instance.Get(value.GetType());
                if (manager != null)
                {
                    manager.Write(key, value, this);
                    mustSerialize = false;   
                }
            }

            if (mustSerialize)
                _state[key] = JsonSerializer.SerializeToNode(value);
        }

        public readonly bool IsRef(string key)
        {
            var item = _state[key];
            return item is JsonValue obj;
        }

        public readonly bool Contains(string key)
        {
            if (_state == null)
                return false;
            return _state.ContainsKey(key);
        }

        public readonly string AsJson()
        {
            return _context.Main.ToJsonString(new JsonSerializerOptions { WriteIndented = true });   
        }

        public readonly IStateContext Context => _context;

        public readonly int Count => _state.Count;

        public readonly IEnumerable<string> Keys => _state.Select(a=> a.Key);
    }
}
