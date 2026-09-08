// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace Oculus.Platform
{
    /// Custom JSON converter that unwraps server responses where a T[] array is
    /// wrapped in a {"data": [...]} object.  When the token is already a bare
    /// array it deserializes directly.  Only matches T[] for the specific type
    /// parameter, e.g. DataArrayConverter&lt;SegmentEvent&gt; handles SegmentEvent[].
    public class DataArrayConverter<T> : JsonConverter
    {
        public DataArrayConverter() { }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(T[]);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JToken token = JToken.Load(reader);

            JArray array = null;

            if (token.Type == JTokenType.Array)
            {
                array = (JArray)token;
            }
            else if (token.Type == JTokenType.Object)
            {
                var dataToken = ((JObject)token)["data"];
                if (dataToken != null && dataToken.Type == JTokenType.Array)
                {
                    array = (JArray)dataToken;
                }
            }

            if (array != null)
            {
                // Deserialize each element as T (not T[]) to avoid re-entering
                // this converter and causing infinite recursion.
                var result = new T[array.Count];
                for (int i = 0; i < array.Count; i++)
                {
                    result[i] = array[i].ToObject<T>(serializer);
                }
                return result;
            }

            throw new JsonSerializationException(
                $"Cannot deserialize token of type '{token.Type}' into '{typeof(T[])}'. " +
                "Expected a JSON array or an object with a 'data' array property.");
        }

        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
