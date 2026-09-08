// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using System;
using Newtonsoft.Json;
/// Custom JSON converter that handles empty strings when deserializing to UInt64.
/// When an empty string is encountered, it will use 0 as the default value.
public class UInt64EmptyStringConverter : JsonConverter<UInt64>
{
    public UInt64EmptyStringConverter() { }

    public override void WriteJson(JsonWriter writer, UInt64 value, JsonSerializer serializer)
    {
        writer.WriteValue(value);
    }

    public override UInt64 ReadJson(JsonReader reader, Type objectType, UInt64 existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.String)
        {
            string stringValue = (string)reader.Value;
            if (string.IsNullOrEmpty(stringValue))
            {
                // Return 0 for empty strings
                return 0;
            }

            // Try to parse the string value
            if (UInt64.TryParse(stringValue, out UInt64 result))
            {
                return result;
            }

            throw new JsonSerializationException($"Unable to convert '{stringValue}' to UInt64.");
        }
        else if (reader.TokenType == JsonToken.Integer)
        {
            return Convert.ToUInt64(reader.Value);
        }
        else if (reader.TokenType == JsonToken.Null)
        {
            return 0;
        }

        throw new JsonSerializationException($"Unexpected token type '{reader.TokenType}' when parsing UInt64.");
    }
}
