using System;
using System.Text;
using JetBrains.Annotations;
using Moonlight.Events.Diagnostics;
using Newtonsoft.Json;

namespace Moonlight.Events.Serialization
{
    [PublicAPI]
    public class JsonSerialization : IEventSerialization
    {
        public IEventLogger Logger { get; set; }

        public JsonSerialization(IEventLogger logger)
        {
            Logger = logger;
        }
        
        public byte[] Serialize<T>(T value)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value));
        }

        public object Deserialize(Type type, byte[] buffer)
        {
            return JsonConvert.DeserializeObject(Encoding.UTF8.GetString(buffer), type);
        }

        public T Deserialize<T>(byte[] buffer)
        {
            return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(buffer));
        }
    }
}