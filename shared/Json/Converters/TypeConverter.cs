using System;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Moonlight.Shared.Internal.Json.Converters
{
    [PublicAPI]
    public class TypeConverter : JsonConverter
    {
        public Type DesiredType { get; set; }

        public TypeConverter(Type desiredType)
        {
            DesiredType = desiredType;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return serializer.Deserialize(reader, DesiredType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType) => objectType == DesiredType;
    }
}