// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
/// Custom JSON converter that serializes UInt64 arrays as string arrays.
/// This is needed because some backends expect large integers as strings in JSON.
public class UInt64ArrayAsStringConverter : JsonConverter
{
    public UInt64ArrayAsStringConverter() { }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(UInt64[]);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }

        var array = JArray.Load(reader);
        var result = new UInt64[array.Count];

        for (var i = 0; i < array.Count; i++)
        {
            if (array[i].Type == JTokenType.String)
            {
                result[i] = UInt64.Parse(array[i].ToString());
            }
            else if (array[i].Type == JTokenType.Integer)
            {
                result[i] = array[i].Value<UInt64>();
            }
        }

        return result;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        var array = (UInt64[])value;
        writer.WriteStartArray();

        foreach (var item in array)
        {
            writer.WriteValue(item.ToString());
        }

        writer.WriteEndArray();
    }
}
