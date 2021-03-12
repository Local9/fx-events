using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Moonlight.Shared.Internal.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Moonlight.Shared.Internal.Json.Converters
{
    [PublicAPI]
    public class PreserveTypeConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var converters = serializer.Converters.ToArray();
            var type = value.GetType();

            writer.WriteStartObject();
            writer.WritePropertyName("$type");
            writer.WriteValue(type.AssemblyQualifiedName);

            var token = JToken.FromObject(value);
            var siblings = token.Type == JTokenType.Object && token.HasValues;

            if (token.Type == JTokenType.Array)
            {
                writer.WritePropertyName("$values");
            }
            else if (!siblings)
            {
                writer.WritePropertyName("$value");
            }

            if (siblings)
                token.Children().ForEach(self => self.WriteTo(writer, converters));
            else if (value is Snowflake snowflake)
                writer.WriteValue(snowflake.ToInt64());
            else
                serializer.Serialize(writer, value);

            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.StartObject)
            {
                return GetTypeDenotedValue(objectType, (JObject) JToken.Load(reader), serializer);
            }

            if (reader.TokenType == JsonToken.StartArray)
            {
                var contract = (JsonArrayContract) serializer.ContractResolver.ResolveContract(objectType);
                var itemType = contract.CollectionItemType;
                var existingList = existingValue as IList;
                var list = (IList) new List<object>();

                while (reader.Read())
                {
                    if (reader.TokenType == JsonToken.Comment)
                    {
                        continue;
                    }

                    if (reader.TokenType == JsonToken.Null)
                    {
                        list.Add(null);
                    }
                    else if (reader.TokenType == JsonToken.EndArray)
                    {
                        var array = Array.CreateInstance(itemType!, list.Count);

                        list.CopyTo(array, 0);

                        return array;
                    }
                    else
                    {
                        var existingItem = existingList != null && list.Count < existingList.Count ? existingList[list.Count] : null;

                        if (existingItem == null)
                        {
                            existingItem = GetTypeDenotedValue(itemType, (JObject) JToken.Load(reader), serializer);
                        }
                        else
                        {
                            serializer.Populate(reader, existingItem);
                        }

                        list.Add(existingItem);
                    }
                }

                throw new JsonSerializationException($"Unclosed array at path: {reader.Path}");
            }

            return null;
        }

        private object GetTypeDenotedValue(Type original, JObject token, JsonSerializer serializer)
        {
            if (token.TryGetValue("$type", out var designated))
            {
                var type = Type.GetType(designated.ToString());

                if (type == null) return token.ToObject<object>();

                token.Remove("$type");

                if (token.ContainsKey("$value"))
                {
                    return serializer.Deserialize(token.GetValue("$value")?.CreateReader()!, type);
                }

                if (token.ContainsKey("$values"))
                {
                    return serializer.Deserialize(token.GetValue("$values")?.CreateReader()!, type);
                }

                var instance = Activator.CreateInstance(type);

                serializer.Populate(token.CreateReader(), instance);

                return instance;
            }

            return token.ToObject(original);
        }

        public override bool CanConvert(Type type) => true;
    }
}