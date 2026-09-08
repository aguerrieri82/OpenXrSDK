// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace Oculus.Platform
{
    public class BoolFromObjectConverter : JsonConverter<bool>
    {
        // Fields that identify a LeaderboardUpdateStatus response
        private const string DidUpdateField = "did_update";
        private const string UpdatedChallengeIdsField = "updated_challenge_ids";
        public BoolFromObjectConverter() { }

        public override bool ReadJson(JsonReader reader, Type objectType, bool existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Boolean)
            {
                return (bool)reader.Value;
            }

            if (reader.TokenType == JsonToken.StartObject)
            {
                var jsonObject = JObject.Load(reader);

                // Only convert if this matches the LeaderboardUpdateStatus structure
                if (IsLeaderboardUpdateStatusResponse(jsonObject))
                {
                    return jsonObject[DidUpdateField].Value<bool>();
                }

                // For non-matching objects, throw to indicate unexpected format
                throw new JsonSerializationException(
                    $"Cannot deserialize JSON object to bool. Expected a LeaderboardUpdateStatus response with '{DidUpdateField}' and '{UpdatedChallengeIdsField}' fields.");
            }

            throw new JsonSerializationException(
                $"Unexpected token type '{reader.TokenType}' when parsing bool.");
        }

        public override void WriteJson(JsonWriter writer, bool value, JsonSerializer serializer)
        {
            writer.WriteValue(value);
        }

        private static bool IsLeaderboardUpdateStatusResponse(JObject jsonObject)
        {
            return jsonObject.ContainsKey(DidUpdateField) &&
                   jsonObject.ContainsKey(UpdatedChallengeIdsField) &&
                   jsonObject[DidUpdateField].Type == JTokenType.Boolean;
        }
    }
}
