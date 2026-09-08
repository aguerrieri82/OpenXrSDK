// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Reflection;
public class EnumArrayDescriptionConverter<T> : JsonConverter where T : struct, Enum
{
    public EnumArrayDescriptionConverter() { }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(T[]);
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        T[] array = (T[])value;
        writer.WriteStartArray();

        foreach (T item in array)
        {
            writer.WriteValue(GetDescription(item));
        }

        writer.WriteEndArray();
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }

        var items = new System.Collections.Generic.List<T>();

        if (reader.TokenType == JsonToken.StartArray)
        {
            reader.Read();
            while (reader.TokenType != JsonToken.EndArray)
            {
                var description = (string)reader.Value;
                items.Add(FromDescription(description));
                reader.Read();
            }
        }

        return items.ToArray();
    }

    private string GetDescription(T value)
    {
        FieldInfo fi = value.GetType().GetField(value.ToString());
        if (fi != null)
        {
            var attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
            if (attributes != null && attributes.Length > 0)
            {
                return attributes[0].Description;
            }
        }
        return value.ToString();
    }

    private T FromDescription(string description)
    {
        foreach (var field in typeof(T).GetFields())
        {
            if (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attribute)
            {
                if (attribute.Description == description)
                    return (T)field.GetValue(null);
            }
            else
            {
                if (field.Name == description)
                    return (T)field.GetValue(null);
            }
        }

        throw new ArgumentException($"Unknown description '{description}' for enum '{typeof(T)}'");
    }
}
