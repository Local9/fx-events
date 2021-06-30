using System;
using System.Text;
using JetBrains.Annotations;
using Moonlight.Events.Diagnostics;
using Newtonsoft.Json;

namespace Moonlight.Events.Serialization.Implementations
{
    [PublicAPI]
    public class JsonSerialization : ISerialization
    {
        public IEventLogger Logger { get; set; }

        public JsonSerialization(IEventLogger logger)
        {
            Logger = logger;
        }

        public void Serialize<T>(T value, SerializationContext context)
        {
            context.Writer.Write(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value)));
        }

        public object Deserialize(Type type, SerializationContext context)
        {
            return JsonConvert.DeserializeObject(Encoding.UTF8.GetString(context.Reader.ReadBytes(context.Original!.Length)), type);
        }

        public T Deserialize<T>(SerializationContext context)
        {
            return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(context.Reader.ReadBytes(context.Original!.Length)));
        }
    }
}