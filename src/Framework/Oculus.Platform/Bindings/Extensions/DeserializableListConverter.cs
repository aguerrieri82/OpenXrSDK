// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
/// Custom JSON converter that handles both array and object formats when deserializing DeserializableList.
/// Supports both direct array format [] and object format with data/paging properties.
public class DeserializableListConverter : JsonConverter
{
    public DeserializableListConverter() { }
    public override bool CanConvert(Type objectType)
    {
        Type type = objectType;
        while (type != null)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(DeserializableList<>))
                return true;
            type = type.BaseType;
        }
        return false;
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        JToken token = JToken.Load(reader);

        object result = Activator.CreateInstance(objectType);
        var dataProperty = objectType.GetProperty("Data");
        var pagingProperty = objectType.GetProperty("Paging");
        var summaryProperty = objectType.GetProperty("Summary");

        if (token.Type == JTokenType.Array)
        {
            var listData = token.ToObject(dataProperty.PropertyType, serializer);
            dataProperty.SetValue(result, listData);
            pagingProperty.SetValue(result, new PagingJSON());
            summaryProperty.SetValue(result, new SummaryJSON());
        }
        else if (token.Type == JTokenType.Object)
        {
            var dataToken = token["data"];
            var pagingToken = token["paging"];
            var summaryToken = token["summary"];

            if (dataProperty != null && dataToken != null)
            {
                var listData = dataToken.ToObject(dataProperty.PropertyType, serializer);
                dataProperty.SetValue(result, listData);
            }

            if (pagingProperty != null && pagingToken != null)
            {
                var paging = pagingToken.ToObject<PagingJSON>(serializer);
                pagingProperty.SetValue(result, paging);
            }

            if (summaryProperty != null && summaryToken != null)
            {
                var summary = summaryToken.ToObject<SummaryJSON>(serializer);
                summaryProperty.SetValue(result, summary);
            }
        }

        return result;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        Type objectType = value.GetType();
        var dataProperty = objectType.GetProperty("Data");
        var pagingProperty = objectType.GetProperty("Paging");
        var summaryProperty = objectType.GetProperty("Summary");

        var data = dataProperty.GetValue(value);
        var paging = pagingProperty.GetValue(value);
        var summary = summaryProperty.GetValue(value);

        writer.WriteStartObject();

        writer.WritePropertyName("data");
        serializer.Serialize(writer, data);

        writer.WritePropertyName("paging");
        serializer.Serialize(writer, paging);

        writer.WritePropertyName("summary");
        serializer.Serialize(writer, summary);

        writer.WriteEndObject();
    }
}
