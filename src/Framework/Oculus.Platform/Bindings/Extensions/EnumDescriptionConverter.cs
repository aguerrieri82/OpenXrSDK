// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
public class EnumDescriptionConverter<T> : JsonConverter<T> where T : struct, Enum
{
    public EnumDescriptionConverter() { }
    public override void WriteJson(JsonWriter writer, T value, JsonSerializer serializer)
    {
        writer.WriteValue(GetDescription(value));
    }

    public override T ReadJson(JsonReader reader, Type objectType, T existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var description = (string)reader.Value;

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
}
