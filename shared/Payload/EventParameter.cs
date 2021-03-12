using System;
using JetBrains.Annotations;
using Moonlight.Shared.Internal.Json;
using Newtonsoft.Json;

namespace Moonlight.Shared.Internal.Events.Payload
{
    [PublicAPI]
    public class EventParameter
    {
        [JsonProperty("_")] public string Serialized { get; set; }

        [JsonConstructor]
        public EventParameter(string serialized)
        {
            Serialized = serialized;
        }

        public EventParameter(object value)
        {
            Serialized = value.ToJson();
        }

        public object Deserialize(Type type)
        {
            return Serialized.FromJson(type);
        }

        public T Deserialize<T>() => (T) Deserialize(typeof(T));
    }
}